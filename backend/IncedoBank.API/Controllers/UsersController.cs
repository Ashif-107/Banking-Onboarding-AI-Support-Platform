// Controllers/UsersController.cs
// User sync with Clerk & profile management

using IncedoBank.API.Domain;
using IncedoBank.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IncedoBank.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(AppDbContext db, ILogger<UsersController> logger) : ControllerBase
{
    private string ClerkUserId => User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    // POST /api/users/sync — Sync user authenticated via Clerk
    [HttpPost("sync")]
    public async Task<IActionResult> SyncUser([FromBody] SyncUserRequest req, CancellationToken ct = default)
    {
        var existing = await db.Customers
            .Include(c => c.Profile)
            .FirstOrDefaultAsync(c => c.ClerkUserId == req.ClerkUserId, ct);

        if (existing is null)
        {
            var customer = new Customer
            {
                ClerkUserId = req.ClerkUserId,
                Email = req.Email,
                Phone = req.Phone,
                SecurityLevel = 1,
                Status = "active"
            };

            var profile = new Profile
            {
                CustomerId = customer.Id
            };

            db.Customers.Add(customer);
            db.Profiles.Add(profile);
            await db.SaveChangesAsync(ct);

            logger.LogInformation("New customer synced: {ClerkUserId}", req.ClerkUserId);
            return Ok(new { customer.Id, isNew = true });
        }

        // Update phone if provided
        if (!string.IsNullOrEmpty(req.Phone) && existing.Phone != req.Phone)
        {
            existing.Phone = req.Phone;
            await db.SaveChangesAsync(ct);
        }

        return Ok(new { existing.Id, isNew = false });
    }

    // GET /api/users/profile
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct = default)
    {
        var customer = await db.Customers
            .Include(c => c.Profile)
            .Include(c => c.KycRecord)
            .FirstOrDefaultAsync(c => c.ClerkUserId == ClerkUserId, ct);

        if (customer is null) return NotFound("User profile not found");

        return Ok(new
        {
            customer.Id,
            customer.Email,
            customer.Phone,
            customer.SecurityLevel,
            customer.Status,
            Profile = customer.Profile is not null ? new
            {
                customer.Profile.FullName,
                customer.Profile.Dob,
                PanNumber = MaskPan(customer.Profile.PanNumber),
                HasAadhaar = !string.IsNullOrEmpty(customer.Profile.AadhaarHash),
                customer.Profile.Gender,
                customer.Profile.Occupation,
                customer.Profile.AnnualIncome,
                customer.Profile.Address
            } : null,
            KycStatus = customer.KycRecord?.Status ?? "not_started"
        });
    }

    // PUT /api/users/profile
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req, CancellationToken ct = default)
    {
        var customer = await db.Customers
            .Include(c => c.Profile)
            .FirstOrDefaultAsync(c => c.ClerkUserId == ClerkUserId, ct);

        if (customer is null) return NotFound();

        if (customer.Profile is null)
        {
            customer.Profile = new Profile { CustomerId = customer.Id };
            db.Profiles.Add(customer.Profile);
        }

        if (!string.IsNullOrEmpty(req.FullName)) customer.Profile.FullName = req.FullName;
        if (req.Dob.HasValue) customer.Profile.Dob = req.Dob;
        if (!string.IsNullOrEmpty(req.Gender)) customer.Profile.Gender = req.Gender;
        if (!string.IsNullOrEmpty(req.Occupation)) customer.Profile.Occupation = req.Occupation;
        if (req.AnnualIncome.HasValue) customer.Profile.AnnualIncome = req.AnnualIncome;
        if (req.Address is not null) customer.Profile.Address = req.Address;

        customer.Profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Ok(new { message = "Profile updated successfully" });
    }

    private static string? MaskPan(string? pan)
    {
        if (string.IsNullOrEmpty(pan) || pan.Length < 10) return pan;
        return $"{pan[..2]}XXXXXX{pan[^2..]}";
    }
}

public record SyncUserRequest(string ClerkUserId, string Email, string? Phone);
public record UpdateProfileRequest(
    string? FullName,
    DateOnly? Dob,
    string? Gender,
    string? Occupation,
    long? AnnualIncome,
    Dictionary<string, string>? Address
);
