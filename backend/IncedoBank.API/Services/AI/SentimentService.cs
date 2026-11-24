// Services/AI/SentimentService.cs
// Free sentiment analysis via HuggingFace Inference API

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IncedoBank.API.Services.AI;

public interface ISentimentService
{
    Task<SentimentResult> AnalyzeAsync(string text, CancellationToken ct = default);
}

public record SentimentResult(
    string Label,           // positive|negative|neutral
    decimal Score,          // 0.000 to 1.000
    decimal ChurnRisk,      // computed 0.000 to 1.000
    EmotionBreakdown Emotions
);

public record EmotionBreakdown(
    decimal Anger,
    decimal Joy,
    decimal Sadness,
    decimal Fear,
    decimal Surprise,
    decimal Neutral
);

public class SentimentService(IHttpClientFactory httpFactory, IConfiguration config, ILogger<SentimentService> logger) : ISentimentService
{
    // Free HuggingFace models:
    // Sentiment: cardiffnlp/twitter-roberta-base-sentiment-latest
    // Emotion:   j-hartmann/emotion-english-distilroberta-base
    private const string SentimentModel = "cardiffnlp/twitter-roberta-base-sentiment-latest";
    private const string EmotionModel = "j-hartmann/emotion-english-distilroberta-base";
    private const string HfBaseUrl = "https://api-inference.huggingface.co/models/";

    public async Task<SentimentResult> AnalyzeAsync(string text, CancellationToken ct = default)
    {
        var (sentimentLabel, sentimentScore) = await GetSentimentAsync(text, ct);
        var emotions = await GetEmotionsAsync(text, ct);
        var churnRisk = ComputeChurnRisk(sentimentLabel, sentimentScore, emotions);

        return new SentimentResult(sentimentLabel, sentimentScore, churnRisk, emotions);
    }

    private async Task<(string label, decimal score)> GetSentimentAsync(string text, CancellationToken ct)
    {
        try
        {
            var client = CreateClient();
            var payload = JsonSerializer.Serialize(new { inputs = text[..Math.Min(text.Length, 512)] });
            var response = await client.PostAsync(
                $"{HfBaseUrl}{SentimentModel}",
                new StringContent(payload, Encoding.UTF8, "application/json"),
                ct);

            if (!response.IsSuccessStatusCode) return ("neutral", 0.5m);

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);

            // HuggingFace returns [[{label, score}, ...]]
            var results = doc.RootElement[0].EnumerateArray()
                .Select(r => (
                    Label: r.GetProperty("label").GetString() ?? "neutral",
                    Score: r.GetProperty("score").GetDecimal()
                ))
                .OrderByDescending(r => r.Score)
                .First();

            var normalizedLabel = results.Label.ToLower() switch
            {
                "positive" or "label_2" => "positive",
                "negative" or "label_0" => "negative",
                _ => "neutral"
            };

            return (normalizedLabel, results.Score);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "HuggingFace sentiment API failed, using neutral fallback");
            return ("neutral", 0.5m);
        }
    }

    private async Task<EmotionBreakdown> GetEmotionsAsync(string text, CancellationToken ct)
    {
        try
        {
            var client = CreateClient();
            var payload = JsonSerializer.Serialize(new { inputs = text[..Math.Min(text.Length, 512)] });
            var response = await client.PostAsync(
                $"{HfBaseUrl}{EmotionModel}",
                new StringContent(payload, Encoding.UTF8, "application/json"),
                ct);

            if (!response.IsSuccessStatusCode) return new EmotionBreakdown(0, 0, 0, 0, 0, 1);

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);

            var emotions = doc.RootElement[0].EnumerateArray()
                .ToDictionary(
                    r => r.GetProperty("label").GetString()?.ToLower() ?? "",
                    r => r.GetProperty("score").GetDecimal()
                );

            return new EmotionBreakdown(
                Anger:    emotions.GetValueOrDefault("anger",    0),
                Joy:      emotions.GetValueOrDefault("joy",      0),
                Sadness:  emotions.GetValueOrDefault("sadness",  0),
                Fear:     emotions.GetValueOrDefault("fear",     0),
                Surprise: emotions.GetValueOrDefault("surprise", 0),
                Neutral:  emotions.GetValueOrDefault("neutral",  0)
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "HuggingFace emotion API failed, using neutral fallback");
            return new EmotionBreakdown(0, 0, 0, 0, 0, 1);
        }
    }

    /// <summary>
    /// Rule-based churn risk formula — zero ML cost:
    /// churn_risk = (negative_sentiment_weight × 0.6) + (anger_emotion × 0.3) + (fear_emotion × 0.1)
    /// </summary>
    private static decimal ComputeChurnRisk(string label, decimal sentimentScore, EmotionBreakdown emotions)
    {
        var negativeSentimentWeight = label == "negative" ? sentimentScore : 0m;
        var churnRisk = (negativeSentimentWeight * 0.6m) + (emotions.Anger * 0.3m) + (emotions.Fear * 0.1m);
        return Math.Min(1m, Math.Round(churnRisk, 3));
    }

    private HttpClient CreateClient()
    {
        var client = httpFactory.CreateClient("huggingface");
        var apiKey = config["HuggingFace:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        return client;
    }
}
