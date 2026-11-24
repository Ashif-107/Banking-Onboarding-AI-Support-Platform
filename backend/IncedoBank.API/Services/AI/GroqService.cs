// Services/AI/GroqService.cs
// Free AI via Groq API — Llama 3.3 70B

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IncedoBank.API.Services.AI;

public interface IGroqService
{
    Task<TicketSummaryResult> SummarizeTicketAsync(string ticketText, CancellationToken ct = default);
    Task<SuggestedResponsesResult> GenerateResponsesAsync(string ticketText, string? summary, List<string> policyChunks, CancellationToken ct = default);
    Task<EligibilityResult> ScreenEligibilityAsync(string productName, object profileData, object eligibilityRules, CancellationToken ct = default);
}

public record TicketSummaryResult(
    string Summary,
    string IssueType,
    string Urgency,
    string[] Tags,
    int ComplexityScore,
    string SentimentHint
);

public record SuggestedResponsesResult(
    string FormalDraft,
    string EmpatheticDraft,
    string ConciseDraft
);

public record EligibilityResult(
    int Score,
    string Recommendation,
    string[] Reasons
);

public class GroqService(IHttpClientFactory httpFactory, IConfiguration config, ILogger<GroqService> logger) : IGroqService
{
    private const string BaseUrl = "https://api.groq.com/openai/v1/chat/completions";
    private const string Model = "llama-3.3-70b-versatile";

    private HttpClient CreateClient()
    {
        var client = httpFactory.CreateClient("groq");
        var apiKey = config["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq:ApiKey not configured");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        return client;
    }

    public async Task<TicketSummaryResult> SummarizeTicketAsync(string ticketText, CancellationToken ct = default)
    {
        const string systemPrompt = """
            You are a banking support ticket summarizer. Analyze the ticket and return ONLY valid JSON with these fields:
            {
              "summary": "2-3 sentence summary of the customer's issue",
              "issue_type": "one of: account_issue|payment|kyc|loan|card|general",
              "urgency": "one of: low|medium|high|critical",
              "tags": ["array", "of", "relevant", "tags"],
              "complexity_score": 1-10,
              "sentiment_hint": "one of: positive|neutral|frustrated|angry"
            }
            Be concise and accurate. No explanations, only JSON.
            """;

        var result = await CallGroqAsync(systemPrompt, ticketText, ct);

        try
        {
            var json = JsonDocument.Parse(result).RootElement;
            return new TicketSummaryResult(
                json.GetProperty("summary").GetString() ?? "",
                json.GetProperty("issue_type").GetString() ?? "general",
                json.GetProperty("urgency").GetString() ?? "medium",
                json.GetProperty("tags").EnumerateArray().Select(t => t.GetString() ?? "").ToArray(),
                json.GetProperty("complexity_score").GetInt32(),
                json.GetProperty("sentiment_hint").GetString() ?? "neutral"
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse Groq summarization response");
            return new TicketSummaryResult(result, "general", "medium", [], 5, "neutral");
        }
    }

    public async Task<SuggestedResponsesResult> GenerateResponsesAsync(
        string ticketText, string? summary, List<string> policyChunks, CancellationToken ct = default)
    {
        var context = policyChunks.Count > 0
            ? $"\n\nRelevant policy context:\n{string.Join("\n---\n", policyChunks)}"
            : "";

        var systemPrompt = $"""
            You are an expert banking support agent. Generate 3 response drafts for the customer's issue.
            Return ONLY valid JSON:{context}
            {{
              "formal": "Professional, formal tone response (2-3 paragraphs)",
              "empathetic": "Warm, understanding tone response acknowledging the customer's frustration",
              "concise": "Brief, direct response in 2-3 sentences"
            }}
            All responses must be accurate, compliant with banking regulations, and not disclose internal policies.
            """;

        var userMessage = $"Ticket: {ticketText}\n\nSummary: {summary ?? "N/A"}";
        var result = await CallGroqAsync(systemPrompt, userMessage, ct);

        try
        {
            var json = JsonDocument.Parse(result).RootElement;
            return new SuggestedResponsesResult(
                json.GetProperty("formal").GetString() ?? "",
                json.GetProperty("empathetic").GetString() ?? "",
                json.GetProperty("concise").GetString() ?? ""
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse Groq response generation");
            return new SuggestedResponsesResult(result, result, result);
        }
    }

    public async Task<EligibilityResult> ScreenEligibilityAsync(
        string productName, object profileData, object eligibilityRules, CancellationToken ct = default)
    {
        const string systemPrompt = """
            You are a banking eligibility pre-screener. Assess customer eligibility and return ONLY valid JSON:
            {
              "score": 0-100,
              "recommendation": "one of: highly_eligible|eligible|borderline|not_eligible",
              "reasons": ["array", "of", "3-5", "specific", "reasons"]
            }
            Be objective and accurate based on the provided profile data and eligibility rules.
            """;

        var userMessage = $"""
            Product: {productName}
            Customer Profile: {JsonSerializer.Serialize(profileData)}
            Eligibility Rules: {JsonSerializer.Serialize(eligibilityRules)}
            """;

        var result = await CallGroqAsync(systemPrompt, userMessage, ct);

        try
        {
            var json = JsonDocument.Parse(result).RootElement;
            return new EligibilityResult(
                json.GetProperty("score").GetInt32(),
                json.GetProperty("recommendation").GetString() ?? "borderline",
                json.GetProperty("reasons").EnumerateArray().Select(r => r.GetString() ?? "").ToArray()
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse Groq eligibility response");
            return new EligibilityResult(50, "borderline", ["Unable to assess eligibility automatically."]);
        }
    }

    private async Task<string> CallGroqAsync(string systemPrompt, string userMessage, CancellationToken ct)
    {
        var client = CreateClient();

        var requestBody = new
        {
            model = Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userMessage }
            },
            response_format = new { type = "json_object" },
            temperature = 0.3,
            max_tokens = 1024
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(BaseUrl, content, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(responseJson);

        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "{}";
    }
}
