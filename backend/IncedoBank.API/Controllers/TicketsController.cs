// Controllers/TicketsController.cs
// Full AI-powered ticket management + Agent Cockpit APIs

using IncedoBank.API.Domain;
using IncedoBank.API.Infrastructure.Persistence;
using IncedoBank.API.Services.AI;
using IncedoBank.API.Services.NBA;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IncedoBank.API.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
public class TicketsController(
    AppDbContext db,
    IGroqService groq,
    ISentimentService sentiment,
    INBAService nba,
    ILogger<TicketsController> logger) : ControllerBase
{
    private string ClerkUserId => User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    // GET /api/tickets
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] int page = 1, CancellationToken ct = default)
    {
        var query = db.Tickets.AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);

        var total = await query.CountAsync(ct);
        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * 20)
            .Take(20)
            .Select(t => new
            {
                t.Id, t.Subject, t.Channel, t.Status,
                t.SentimentLabel, t.ChurnRisk, t.ComplexityScore,
                t.Tags, t.AiSummary, t.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new { data = tickets, total, page });
    }

    // GET /api/tickets/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var ticket = await db.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Responses)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (ticket is null) return NotFound();
        return Ok(ticket);
    }

    // POST /api/tickets
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTicketRequest req, CancellationToken ct = default)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.ClerkUserId == ClerkUserId, ct);

        var ticket = new Ticket
        {
            CustomerId = customer?.Id,
            Channel = req.Channel,
            Subject = req.Subject,
            RawText = req.RawText,
            Status = "open"
        };

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);

        // Run sentiment analysis in background
        _ = Task.Run(async () =>
        {
            try
            {
                var result = await sentiment.AnalyzeAsync(ticket.RawText);
                ticket.SentimentLabel = result.Label;
                ticket.SentimentScore = result.Score;
                ticket.ChurnRisk = result.ChurnRisk;
                ticket.EmotionBreakdown = new Dictionary<string, decimal>
                {
                    ["anger"] = result.Emotions.Anger,
                    ["joy"] = result.Emotions.Joy,
                    ["sadness"] = result.Emotions.Sadness,
                    ["fear"] = result.Emotions.Fear,
                    ["surprise"] = result.Emotions.Surprise,
                    ["neutral"] = result.Emotions.Neutral
                };
                await db.SaveChangesAsync();
            }
            catch (Exception ex) { logger.LogError(ex, "Background sentiment failed for ticket {Id}", ticket.Id); }
        }, CancellationToken.None);

        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, new { ticket.Id });
    }

    // POST /api/tickets/{id}/summarize
    [HttpPost("{id:guid}/summarize")]
    public async Task<IActionResult> Summarize(Guid id, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.FindAsync([id], ct);
        if (ticket is null) return NotFound();

        var result = await groq.SummarizeTicketAsync(ticket.RawText, ct);

        ticket.AiSummary = result.Summary;
        ticket.Tags = result.Tags;
        ticket.ComplexityScore = result.ComplexityScore;
        await db.SaveChangesAsync(ct);

        return Ok(result);
    }

    // GET /api/tickets/{id}/responses/suggest
    [HttpGet("{id:guid}/responses/suggest")]
    public async Task<IActionResult> SuggestResponses(Guid id, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.FindAsync([id], ct);
        if (ticket is null) return NotFound();

        // Fetch relevant policy chunks (simple keyword match — replace with pgvector for RAG)
        var policyChunks = await db.PolicyChunks
            .Where(p => p.ChunkText.Contains(ticket.Tags != null && ticket.Tags.Length > 0
                ? ticket.Tags[0] : "general"))
            .Take(3)
            .Select(p => p.ChunkText)
            .ToListAsync(ct);

        var result = await groq.GenerateResponsesAsync(ticket.RawText, ticket.AiSummary, policyChunks, ct);
        return Ok(result);
    }

    // POST /api/tickets/{id}/responses
    [HttpPost("{id:guid}/responses")]
    public async Task<IActionResult> SendResponse(Guid id, [FromBody] SendResponseRequest req, CancellationToken ct = default)
    {
        var ticket = await db.Tickets.FindAsync([id], ct);
        if (ticket is null) return NotFound();

        var response = new TicketResponse
        {
            TicketId = id,
            AgentId = ClerkUserId,
            AiDraft = req.AiDraft,
            FinalResponse = req.FinalResponse,
            ToneUsed = req.Tone,
            WasEdited = req.AiDraft != null && req.AiDraft != req.FinalResponse
        };

        db.TicketResponses.Add(response);
        ticket.Status = "in_progress";
        await db.SaveChangesAsync(ct);

        return Ok(new { response.Id });
    }

    // GET /api/tickets/{id}/nba
    [HttpGet("{id:guid}/nba")]
    public async Task<IActionResult> GetNBA(Guid id, CancellationToken ct = default)
    {
        var ticket = await db.Tickets
            .Include(t => t.Customer)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (ticket is null) return NotFound();
        if (ticket.Customer is null) return Ok(new { actions = new List<NBAAction>() });

        var actions = await nba.GetActionsAsync(ticket, ticket.Customer, ct);
        return Ok(new { actions });
    }
}

public record CreateTicketRequest(string RawText, string Subject, string Channel = "web");
public record SendResponseRequest(string FinalResponse, string Tone, string? AiDraft = null);
