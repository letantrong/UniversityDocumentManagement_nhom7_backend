using Application.Features.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly ISender _sender;

    public PermissionsController(ISender sender)
    {
        _sender = sender;
    }
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPermissionsQuery(), cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

   [HttpGet("documents/{documentId:guid}/access")]
    public async Task<IActionResult> GetDocumentAccess(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDocumentAccessQuery(documentId), cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet("documents/{documentId:guid}/check")]
    public async Task<IActionResult> CheckDocumentPermission(
        Guid documentId,
        [FromQuery] DocumentPermissionAction action,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CheckDocumentPermissionQuery(documentId, action),
            cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}