// Domain/Models — Core entities

namespace IncedoBank.API.Domain;

// ---- Customer ----
public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ClerkUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int SecurityLevel { get; set; } = 1;
    public string Status { get; set; } = "pending_kyc";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Profile? Profile { get; set; }
    public KycRecord? KycRecord { get; set; }
    public ICollection<Application> Applications { get; set; } = [];
    public ICollection<Ticket> Tickets { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
}

// ---- Profile ----
public class Profile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public string? FullName { get; set; }
    public DateOnly? Dob { get; set; }
    public string? PanNumber { get; set; }         // masked/validated only
    public string? AadhaarHash { get; set; }       // SHA-256 hash only
    public string? Gender { get; set; }
    public string? Occupation { get; set; }
    public long? AnnualIncome { get; set; }
    public Dictionary<string, string>? Address { get; set; }
    public Dictionary<string, object>? OcrExtractedData { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Customer? Customer { get; set; }
}

// ---- KYC Record ----
public class KycRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = "pending"; // pending|in_review|approved|rejected
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Customer? Customer { get; set; }
    public ICollection<KycDocument> Documents { get; set; } = [];
}

// ---- KYC Document ----
public class KycDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Guid KycRecordId { get; set; }
    public string DocType { get; set; } = string.Empty; // aadhaar|pan|passport|payslip|selfie
    public string R2ObjectKey { get; set; } = string.Empty;
    public long? FileSizeBytes { get; set; }
    public string? MimeType { get; set; }
    public string? OcrText { get; set; }
    public decimal? OcrConfidence { get; set; }
    public bool? FaceDetected { get; set; }
    public string Status { get; set; } = "uploaded";
    public int Version { get; set; } = 1;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Customer? Customer { get; set; }
    public KycRecord? KycRecord { get; set; }
}

// ---- Product ----
public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Dictionary<string, object> FormSchema { get; set; } = [];
    public Dictionary<string, object>? EligibilityRules { get; set; }
    public int SlaHours { get; set; } = 72;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Application> Applications { get; set; } = [];
}

// ---- Application ----
public class Application
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public string Status { get; set; } = "draft"; // draft|submitted|in_review|approved|rejected
    public Dictionary<string, object> FormData { get; set; } = [];
    public int? AiEligibilityScore { get; set; }
    public string? AiEligibilityReason { get; set; }
    public DateTime? SlaDeadline { get; set; }
    public string? CurrentStage { get; set; }
    public string? AssignedTo { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Customer? Customer { get; set; }
    public Product? Product { get; set; }
    public ICollection<WorkflowStage> WorkflowStages { get; set; } = [];
}

// ---- Workflow Stage ----
public class WorkflowStage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApplicationId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public int StageOrder { get; set; }
    public string Status { get; set; } = "pending";
    public string? AssigneeId { get; set; }
    public string? Notes { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Application? Application { get; set; }
}

// ---- Ticket ----
public class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CustomerId { get; set; }
    public string? AssignedAgentId { get; set; }
    public string Channel { get; set; } = "web";
    public string? Subject { get; set; }
    public string RawText { get; set; } = string.Empty;
    public string? AiSummary { get; set; }
    public string[]? Tags { get; set; }
    public string Language { get; set; } = "en";
    public string? SentimentLabel { get; set; }
    public decimal? SentimentScore { get; set; }
    public Dictionary<string, decimal>? EmotionBreakdown { get; set; }
    public decimal? ChurnRisk { get; set; }
    public int? ComplexityScore { get; set; }
    public string Status { get; set; } = "open";
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Customer? Customer { get; set; }
    public ICollection<TicketResponse> Responses { get; set; } = [];
}

// ---- Ticket Response ----
public class TicketResponse
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public string AgentId { get; set; } = string.Empty;
    public string? AiDraft { get; set; }
    public string FinalResponse { get; set; } = string.Empty;
    public string? ToneUsed { get; set; }
    public bool WasEdited { get; set; } = false;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public Ticket? Ticket { get; set; }
}

// ---- Policy Chunk (RAG) ----
public class PolicyChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DocumentName { get; set; } = string.Empty;
    public string? Section { get; set; }
    public string ChunkText { get; set; } = string.Empty;
    public Pgvector.Vector? Embedding { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ---- Notification ----
public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Customer? Customer { get; set; }
}

// ---- Audit Event ----
public class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string ActorId { get; set; } = string.Empty;
    public string? ActorRole { get; set; }
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object>? OldValues { get; set; }
    public Dictionary<string, object>? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
