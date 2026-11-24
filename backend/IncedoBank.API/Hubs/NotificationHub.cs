// Hubs/NotificationHub.cs
// SignalR real-time hub for live status updates

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace IncedoBank.API.Hubs;

[Authorize]
public class NotificationHub(ILogger<NotificationHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        logger.LogInformation("User {UserId} connected to NotificationHub", userId);

        // Add user to their personal group so we can send targeted messages
        if (userId != null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        logger.LogInformation("User {UserId} disconnected from NotificationHub", userId);
        await base.OnDisconnectedAsync(exception);
    }

    // Client can join a specific application update stream
    public async Task SubscribeToApplication(string applicationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"application-{applicationId}");
    }

    public async Task UnsubscribeFromApplication(string applicationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"application-{applicationId}");
    }
}

// Helper service to push notifications from anywhere in the app
public interface INotificationPusher
{
    Task PushApplicationStatusAsync(string userId, string applicationId, string status, string stage);
    Task PushNotificationAsync(string userId, string title, string body, string type);
    Task PushTicketUpdateAsync(string agentId, string ticketId, string status);
}

public class NotificationPusher(IHubContext<NotificationHub> hubContext) : INotificationPusher
{
    public async Task PushApplicationStatusAsync(string userId, string applicationId, string status, string stage)
    {
        await hubContext.Clients.Group($"user-{userId}")
            .SendAsync("ApplicationStatusChanged", new { applicationId, status, stage });

        await hubContext.Clients.Group($"application-{applicationId}")
            .SendAsync("ApplicationStatusChanged", new { applicationId, status, stage });
    }

    public async Task PushNotificationAsync(string userId, string title, string body, string type)
    {
        await hubContext.Clients.Group($"user-{userId}")
            .SendAsync("NewNotification", new { title, body, type });
    }

    public async Task PushTicketUpdateAsync(string agentId, string ticketId, string status)
    {
        await hubContext.Clients.Group($"user-{agentId}")
            .SendAsync("TicketUpdated", new { ticketId, status });
    }
}
