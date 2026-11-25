using Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;
using WebApi.Hubs;

namespace WebApi.Services;

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task SendToUserAsync(
        Guid userId,
        string title,
        string message,
        string? referenceId = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new { Title = title, Message = message, ReferenceId = referenceId };
        return _hubContext.Clients
            .Group(NotificationHub.GetUserGroup(userId.ToString()))
            .SendAsync("ReceiveNotification", payload, cancellationToken);
    }

    public Task SendToRoleAsync(
        string roleName,
        string title,
        string message,
        string? referenceId = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new { Title = title, Message = message, ReferenceId = referenceId };
        return _hubContext.Clients
            .Group(NotificationHub.GetRoleGroup(roleName))
            .SendAsync("ReceiveNotification", payload, cancellationToken);
    }

    public Task BroadcastAsync(
        string title,
        string message,
        CancellationToken cancellationToken = default)
    {
        var payload = new { Title = title, Message = message };
        return _hubContext.Clients.All
            .SendAsync("ReceiveNotification", payload, cancellationToken);
    }
}