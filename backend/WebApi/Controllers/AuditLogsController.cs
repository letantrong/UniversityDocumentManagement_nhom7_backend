using Application.Common.Models;
using Application.Features.AuditLogs;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditLogsController : ControllerBase
{
    private readonly ISender _sender;

    public AuditLogsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] PaginationParams pagination,
        [FromQuery] AuditAction? action,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAuditLogsQuery(pagination, action), cancellationToken);
        return result.Succeeded? Ok(result) : BadRequest(result);
    }
}
