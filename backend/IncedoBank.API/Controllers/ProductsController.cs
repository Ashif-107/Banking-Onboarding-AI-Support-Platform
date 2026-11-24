// Controllers/ProductsController.cs
// Banking product catalog & AI eligibility pre-screening

using IncedoBank.API.Domain;
using IncedoBank.API.Infrastructure.Persistence;
using IncedoBank.API.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IncedoBank.API.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(
    AppDbContext db,
    IGroqService groq,
    ILogger<ProductsController> logger) : ControllerBase
{
    private string ClerkUserId => User.FindFirstValue("sub") ?? "";

    // GET /api/products — List all active products
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var products = await db.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Code,
                p.Name,
                p.Description,
                p.FormSchema,
                p.SlaHours
            })
            .ToListAsync(ct);

        return Ok(products);
    }

    // GET /api/products/{id}
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var product = await db.Products.FindAsync([id], ct);
        if (product is null || !product.IsActive) return NotFound();

        return Ok(product);
    }

    // POST /api/products/{id}/screen — AI Eligibility Pre-Screener (0 Enquiries)
    [HttpPost("{id:guid}/screen")]
    [Authorize]
    public async Task<IActionResult> ScreenEligibility(
        Guid id,
        [FromBody] ScreenEligibilityRequest req,
        CancellationToken ct = default)
    {
        var product = await db.Products.FindAsync([id], ct);
        if (product is null) return NotFound();

        var customer = await db.Customers
            .Include(c => c.Profile)
            .FirstOrDefaultAsync(c => c.ClerkUserId == ClerkUserId, ct);

        // Fallback profile if none in database yet
        var profileData = customer?.Profile ?? req.ProfileData;

        var result = await groq.ScreenEligibilityAsync(
            product.Name,
            profileData ?? new object(),
            (object?)product.EligibilityRules ?? new Dictionary<string, object>(),
            ct
        );

        return Ok(result);
    }
}

public record ScreenEligibilityRequest(object? ProfileData);
