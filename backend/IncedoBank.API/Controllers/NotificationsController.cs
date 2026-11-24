// Controllers/NotificationsController.cs
// Customer in-app notifications

using IncedoBank.API.Domain;
using IncedoBank.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IncedoBank.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(AppDbContext db) : ControllerBase
{
    private string ClerkUserId => User.FindFirstValue("sub") ?? "";

    // GET /api/notifications
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.ClerkUserId == ClerkUserId, ct);
        if (customer is null) return NotFound();

        var notifs = await db.Notifications
            .Where(n => n.CustomerId == customer.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        return Ok(notifs);
    }

    // PATCH /api/notifications/{id}/read
    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct = default)
    {
        var notif = await db.Notifications.FindAsync([id], ct);
        if (notif is null) return NotFound();

        notif.IsRead = true;
        await db.SaveChangesAsync(ct);

        return Ok(new { success = true });
    }

    // PATCH /api/notifications/read-all
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct = default)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.ClerkUserId == ClerkUserId, ct);
        if (customer is null) return NotFound();

        await db.Notifications
            .Where(n => n.CustomerId == customer.Id && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);

        return Ok(new { success = true });
    }
}
