using Application.Features.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Common.Models;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool? onlyUnread,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyNotificationsQuery(onlyUnread), cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyNotificationsQuery(true), cancellationToken);
        if (!result.Succeeded)
            return BadRequest(result);

        var count = result.Data?.Count ?? 0;
        return Ok(Result<int>.Success(count));
    }

    [HttpPatch("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new MarkNotificationAsReadCommand(notificationId), cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create(
        [FromBody] CreateNotificationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}