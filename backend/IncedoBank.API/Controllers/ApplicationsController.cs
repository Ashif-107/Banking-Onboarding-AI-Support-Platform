// Controllers/ApplicationsController.cs
// Application lifecycle: Create, Track, Submit, Review

using IncedoBank.API.Domain;
using IncedoBank.API.Hubs;
using IncedoBank.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IncedoBank.API.Controllers;

[ApiController]
[Route("api/applications")]
[Authorize]
public class ApplicationsController(
    AppDbContext db,
    INotificationPusher pusher,
    ILogger<ApplicationsController> logger) : ControllerBase
{
    private string ClerkUserId => User.FindFirstValue("sub") ?? "";

    // GET /api/applications — List customer's applications
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.ClerkUserId == ClerkUserId, ct);
        if (customer is null) return NotFound();

        var applications = await db.Applications
            .Where(a => a.CustomerId == customer.Id)
            .Include(a => a.Product)
            .Include(a => a.WorkflowStages)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                ProductName = a.Product != null ? a.Product.Name : "",
                ProductCode = a.Product != null ? a.Product.Code : "",
                a.Status,
                a.CurrentStage,
                a.SlaDeadline,
                a.AiEligibilityScore,
                a.SubmittedAt,
                a.CreatedAt,
                WorkflowStages = a.WorkflowStages.OrderBy(s => s.StageOrder).Select(s => new
                {
                    s.StageName,
                    s.Status,
                    s.CompletedAt
                })
            })
            .ToListAsync(ct);

        return Ok(applications);
    }

    // GET /api/applications/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var app = await db.Applications
            .Include(a => a.Product)
            .Include(a => a.WorkflowStages)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (app is null) return NotFound();

        return Ok(app);
    }

    // POST /api/applications — Create draft application
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApplicationRequest req, CancellationToken ct = default)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.ClerkUserId == ClerkUserId, ct);
        if (customer is null) return NotFound("Customer profile required");

        var product = await db.Products.FindAsync([req.ProductId], ct);
        if (product is null) return NotFound("Product not found");

        var application = new Application
        {
            CustomerId = customer.Id,
            ProductId = req.ProductId,
            FormData = req.FormData,
            Status = "draft",
            CurrentStage = "draft",
            SlaDeadline = DateTime.UtcNow.AddHours(product.SlaHours)
        };

        db.Applications.Add(application);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Application draft created {Id} for product {Product}", application.Id, product.Code);
        return CreatedAtAction(nameof(GetById), new { id = application.Id }, new { application.Id });
    }

    // POST /api/applications/{id}/submit — Submit application for review
    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct = default)
    {
        var application = await db.Applications
            .Include(a => a.Product)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (application is null) return NotFound();
        if (application.Status != "draft") return BadRequest("Application already submitted");

        application.Status = "submitted";
        application.CurrentStage = "document_verification";
        application.SubmittedAt = DateTime.UtcNow;

        // Initialize default banking workflow stages
        var stages = new List<WorkflowStage>
        {
            new() { ApplicationId = id, StageName = "Document Verification", StageOrder = 1, Status = "in_progress", StartedAt = DateTime.UtcNow },
            new() { ApplicationId = id, StageName = "Risk & Credit Check",   StageOrder = 2, Status = "pending" },
            new() { ApplicationId = id, StageName = "Final Approval",         StageOrder = 3, Status = "pending" }
        };

        db.WorkflowStages.AddRange(stages);
        await db.SaveChangesAsync(ct);

        // Notify customer via SignalR WebSocket in real-time
        await pusher.PushApplicationStatusAsync(
            ClerkUserId,
            application.Id.ToString(),
            application.Status,
            application.CurrentStage
        );

        logger.LogInformation("Application {Id} submitted by user {UserId}", id, ClerkUserId);
        return Ok(new { message = "Application submitted successfully", application.Status, application.CurrentStage });
    }
}

public record CreateApplicationRequest(Guid ProductId, Dictionary<string, object> FormData);
