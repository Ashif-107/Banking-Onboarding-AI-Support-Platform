// Controllers/KYCController.cs

using IncedoBank.API.Domain;
using IncedoBank.API.Infrastructure.Persistence;
using IncedoBank.API.Services.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IncedoBank.API.Controllers;

[ApiController]
[Route("api/kyc")]
[Authorize]
public class KYCController(
    AppDbContext db,
    IStorageService storage,
    ILogger<KYCController> logger) : ControllerBase
{
    private string ClerkUserId => User.FindFirstValue("sub") ?? "";

    // GET /api/kyc/status
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct = default)
    {
        var customer = await db.Customers
            .Include(c => c.KycRecord)
            .ThenInclude(k => k!.Documents)
            .FirstOrDefaultAsync(c => c.ClerkUserId == ClerkUserId, ct);

        if (customer is null) return NotFound("Customer profile not found");

        return Ok(new
        {
            kycStatus = customer.KycRecord?.Status ?? "not_started",
            securityLevel = customer.SecurityLevel,
            documents = customer.KycRecord?.Documents.Select(d => new
            {
                d.DocType, d.Status, d.UploadedAt, d.OcrConfidence
            }) ?? []
        });
    }

    // POST /api/kyc/upload-url — Generate a pre-signed R2 upload URL
    [HttpPost("upload-url")]
    public async Task<IActionResult> GetUploadUrl([FromBody] UploadUrlRequest req, CancellationToken ct = default)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.ClerkUserId == ClerkUserId, ct);
        if (customer is null) return NotFound();

        // Generate a unique, structured key: kyc/{customerId}/{docType}/{timestamp}.{ext}
        var ext = Path.GetExtension(req.FileName).TrimStart('.').ToLower();
        var objectKey = $"kyc/{customer.Id}/{req.DocType}/{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.{ext}";

        var uploadUrl = await storage.GenerateUploadUrlAsync(objectKey, req.MimeType, 15, ct);

        return Ok(new { uploadUrl, objectKey });
    }

    // POST /api/kyc/documents — Confirm upload + save metadata
    [HttpPost("documents")]
    public async Task<IActionResult> ConfirmUpload([FromBody] ConfirmUploadRequest req, CancellationToken ct = default)
    {
        var customer = await db.Customers
            .Include(c => c.KycRecord)
            .FirstOrDefaultAsync(c => c.ClerkUserId == ClerkUserId, ct);

        if (customer is null) return NotFound();

        // Create KYC record if not exists
        if (customer.KycRecord is null)
        {
            customer.KycRecord = new KycRecord { CustomerId = customer.Id };
            db.KycRecords.Add(customer.KycRecord);
            await db.SaveChangesAsync(ct);
        }

        // Check for existing doc of same type (versioning)
        var existing = await db.KycDocuments
            .Where(d => d.CustomerId == customer.Id && d.DocType == req.DocType)
            .OrderByDescending(d => d.Version)
            .FirstOrDefaultAsync(ct);

        var doc = new KycDocument
        {
            CustomerId = customer.Id,
            KycRecordId = customer.KycRecord.Id,
            DocType = req.DocType,
            R2ObjectKey = req.R2ObjectKey,
            OcrText = req.OcrText,
            OcrConfidence = req.OcrConfidence,
            FaceDetected = req.FaceDetected,
            Status = "uploaded",
            Version = (existing?.Version ?? 0) + 1
        };

        db.KycDocuments.Add(doc);
        customer.KycRecord.UpdatedAt = DateTime.UtcNow;

        // Check if all required docs are uploaded → update KYC status
        await db.SaveChangesAsync(ct);
        await UpdateKycStatusAsync(customer.Id, customer.KycRecord.Id, ct);

        logger.LogInformation("KYC document {DocType} uploaded for customer {CustomerId} (v{Version})",
            req.DocType, customer.Id, doc.Version);

        return Ok(new { doc.Id, doc.Status, doc.Version });
    }

    private async Task UpdateKycStatusAsync(Guid customerId, Guid kycRecordId, CancellationToken ct)
    {
        var required = new[] { "aadhaar", "pan", "selfie" };
        var uploaded = await db.KycDocuments
            .Where(d => d.CustomerId == customerId && required.Contains(d.DocType))
            .Select(d => d.DocType)
            .Distinct()
            .ToListAsync(ct);

        if (uploaded.Count >= required.Length)
        {
            var kycRecord = await db.KycRecords.FindAsync([kycRecordId], ct);
            if (kycRecord is not null && kycRecord.Status == "pending")
            {
                kycRecord.Status = "in_review";
                kycRecord.UpdatedAt = DateTime.UtcNow;

                // Bump customer security level
                var customer = await db.Customers.FindAsync([customerId], ct);
                if (customer is not null) customer.SecurityLevel = 2;

                await db.SaveChangesAsync(ct);
            }
        }
    }
}

public record UploadUrlRequest(string DocType, string FileName, string MimeType);
public record ConfirmUploadRequest(
    string DocType,
    string R2ObjectKey,
    string? OcrText,
    decimal? OcrConfidence,
    bool? FaceDetected
);
