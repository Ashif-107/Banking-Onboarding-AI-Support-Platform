// Services/NBA/NBAService.cs
// Next-Best Action rule engine — zero ML cost, .NET pure rule engine

using IncedoBank.API.Domain;

namespace IncedoBank.API.Services.NBA;

public interface INBAService
{
    Task<List<NBAAction>> GetActionsAsync(Ticket ticket, Customer customer, CancellationToken ct = default);
}

public record NBAAction(
    string ActionType,          // upgrade_offer|waive_fee|schedule_call|escalate|retention_offer
    string Title,
    string Description,
    decimal PredictionScore,    // 0.0 to 1.0
    bool CanExecuteOneClick,
    Dictionary<string, object>? ActionData = null
);

public class NBAService(ILogger<NBAService> logger) : INBAService
{
    public Task<List<NBAAction>> GetActionsAsync(Ticket ticket, Customer customer, CancellationToken ct = default)
    {
        var actions = new List<NBAAction>();
        var churnRisk = ticket.ChurnRisk ?? 0m;
        var sentiment = ticket.SentimentLabel ?? "neutral";

        // Rule 1: Escalate if complexity > 7 OR churn risk > 0.7
        if ((ticket.ComplexityScore ?? 0) > 7 || churnRisk > 0.7m)
        {
            actions.Add(new NBAAction(
                "escalate",
                "🚨 Escalate to Senior Agent",
                "High complexity or churn risk detected. Route to specialist immediately.",
                0.95m,
                true
            ));
        }

        // Rule 2: Waive fee for frustrated customers in good standing
        if (sentiment == "frustrated" || sentiment == "angry")
        {
            actions.Add(new NBAAction(
                "waive_fee",
                "💸 Offer Fee Waiver",
                "Customer appears frustrated. A one-time fee waiver may de-escalate the situation.",
                0.78m,
                true,
                new Dictionary<string, object> { ["feeType"] = "processing", ["maxAmount"] = 500 }
            ));
        }

        // Rule 3: Schedule callback for unresolved complex issues
        if ((ticket.ComplexityScore ?? 0) > 5)
        {
            actions.Add(new NBAAction(
                "schedule_call",
                "📞 Schedule Callback",
                "Offer a 30-minute call with a specialist to resolve the issue in detail.",
                0.82m,
                true
            ));
        }

        // Rule 4: Retention offer for high churn risk
        if (churnRisk > 0.5m && customer.SecurityLevel >= 2)
        {
            actions.Add(new NBAAction(
                "retention_offer",
                "🎁 Send Retention Offer",
                "Customer at churn risk. Trigger a personalized retention offer (e.g., interest rate upgrade).",
                0.70m,
                true
            ));
        }

        // Rule 5: Self-service link for common issues
        if (ticket.Tags?.Contains("kyc") == true || ticket.Tags?.Contains("document") == true)
        {
            actions.Add(new NBAAction(
                "self_service_link",
                "🔗 Send KYC Help Link",
                "Share the KYC FAQ and document upload guide to help the customer resolve it themselves.",
                0.88m,
                true,
                new Dictionary<string, object> { ["url"] = "/help/kyc-guide" }
            ));
        }

        // Sort by prediction score descending
        actions.Sort((a, b) => b.PredictionScore.CompareTo(a.PredictionScore));

        logger.LogDebug("Generated {Count} NBA actions for ticket {TicketId}", actions.Count, ticket.Id);
        return Task.FromResult(actions);
    }
}
