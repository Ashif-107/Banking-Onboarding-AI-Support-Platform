// Infrastructure/Persistence/AppDbContext.cs

using IncedoBank.API.Domain;
using Microsoft.EntityFrameworkCore;

namespace IncedoBank.API.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<KycRecord> KycRecords => Set<KycRecord>();
    public DbSet<KycDocument> KycDocuments => Set<KycDocument>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<WorkflowStage> WorkflowStages => Set<WorkflowStage>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketResponse> TicketResponses => Set<TicketResponse>();
    public DbSet<PolicyChunk> PolicyChunks => Set<PolicyChunk>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable pgvector
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.HasPostgresExtension("uuid-ossp");

        // ---- Customer ----
        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("customers");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ClerkUserId).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Id).HasDefaultValueSql("uuid_generate_v4()");
        });

        // ---- Profile ----
        modelBuilder.Entity<Profile>(e =>
        {
            e.ToTable("profiles");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Customer).WithOne(x => x.Profile).HasForeignKey<Profile>(x => x.CustomerId);
            e.Property(x => x.Address).HasColumnType("jsonb");
            e.Property(x => x.OcrExtractedData).HasColumnType("jsonb");
        });

        // ---- KycRecord ----
        modelBuilder.Entity<KycRecord>(e =>
        {
            e.ToTable("kyc_records");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Customer).WithOne(x => x.KycRecord).HasForeignKey<KycRecord>(x => x.CustomerId);
        });

        // ---- KycDocument ----
        modelBuilder.Entity<KycDocument>(e =>
        {
            e.ToTable("kyc_documents");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.KycRecord).WithMany(x => x.Documents).HasForeignKey(x => x.KycRecordId);
        });

        // ---- Product ----
        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.FormSchema).HasColumnType("jsonb");
            e.Property(x => x.EligibilityRules).HasColumnType("jsonb");
        });

        // ---- Application ----
        modelBuilder.Entity<Application>(e =>
        {
            e.ToTable("applications");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Customer).WithMany(x => x.Applications).HasForeignKey(x => x.CustomerId);
            e.HasOne(x => x.Product).WithMany(x => x.Applications).HasForeignKey(x => x.ProductId);
            e.Property(x => x.FormData).HasColumnType("jsonb");
        });

        // ---- WorkflowStage ----
        modelBuilder.Entity<WorkflowStage>(e =>
        {
            e.ToTable("workflow_stages");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Application).WithMany(x => x.WorkflowStages).HasForeignKey(x => x.ApplicationId);
        });

        // ---- Ticket ----
        modelBuilder.Entity<Ticket>(e =>
        {
            e.ToTable("tickets");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Customer).WithMany(x => x.Tickets).HasForeignKey(x => x.CustomerId);
            e.Property(x => x.Tags).HasColumnType("text[]");
            e.Property(x => x.EmotionBreakdown).HasColumnType("jsonb");
        });

        // ---- TicketResponse ----
        modelBuilder.Entity<TicketResponse>(e =>
        {
            e.ToTable("ticket_responses");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Ticket).WithMany(x => x.Responses).HasForeignKey(x => x.TicketId);
        });

        // ---- PolicyChunk (pgvector) ----
        modelBuilder.Entity<PolicyChunk>(e =>
        {
            e.ToTable("policy_chunks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Embedding).HasColumnType("vector(384)");
        });

        // ---- Notification ----
        modelBuilder.Entity<Notification>(e =>
        {
            e.ToTable("notifications");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Customer).WithMany(x => x.Notifications).HasForeignKey(x => x.CustomerId);
            e.Property(x => x.Metadata).HasColumnType("jsonb");
        });

        // ---- AuditEvent (append-only) ----
        modelBuilder.Entity<AuditEvent>(e =>
        {
            e.ToTable("audit_events");
            e.HasKey(x => x.Id);
            e.Property(x => x.OldValues).HasColumnType("jsonb");
            e.Property(x => x.NewValues).HasColumnType("jsonb");
        });
    }
}
