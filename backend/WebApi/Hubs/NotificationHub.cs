using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WebApi.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroup(userId));
        }

        var departmentId = Context.User?.FindFirstValue("departmentId")
                           ?? Context.User?.FindFirstValue("dept_id");
        if (!string.IsNullOrWhiteSpace(departmentId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetDepartmentGroup(departmentId));
        }

        var roles = Context.User?.FindAll(ClaimTypes.Role).Select(c => c.Value)
                    ?? Enumerable.Empty<string>();
        foreach (var role in roles.Where(r => !string.IsNullOrWhiteSpace(r)))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetRoleGroup(role));
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetUserGroup(userId));
        }

        var departmentId = Context.User?.FindFirstValue("departmentId")
                           ?? Context.User?.FindFirstValue("dept_id");
        if (!string.IsNullOrWhiteSpace(departmentId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetDepartmentGroup(departmentId));
        }

        var roles = Context.User?.FindAll(ClaimTypes.Role).Select(c => c.Value)
                    ?? Enumerable.Empty<string>();
        foreach (var role in roles.Where(r => !string.IsNullOrWhiteSpace(r)))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetRoleGroup(role));
        }

        await base.OnDisconnectedAsync(exception);
    }

    public Task JoinDepartmentGroup(string departmentId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GetDepartmentGroup(departmentId));

    public Task LeaveDepartmentGroup(string departmentId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GetDepartmentGroup(departmentId));

    public Task JoinQrSessionGroup(string sessionId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GetQrSessionGroup(sessionId));

    public Task LeaveQrSessionGroup(string sessionId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GetQrSessionGroup(sessionId));

    public Task JoinDashboardGroup() =>
        Groups.AddToGroupAsync(Context.ConnectionId, DashboardGroup);

    public Task LeaveDashboardGroup() =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, DashboardGroup);

    public const string DashboardGroup = "dashboard";

    public static string GetUserGroup(string userId) => $"user:{userId}";
    public static string GetRoleGroup(string roleName) => $"role:{roleName}";
    public static string GetDepartmentGroup(string departmentId) => $"department:{departmentId}";
    public static string GetQrSessionGroup(string sessionId) => $"qr-session:{sessionId}";

    private string? GetUserId() =>
        Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Context.User?.FindFirstValue("sub")
        ?? Context.User?.FindFirstValue("uid");
}