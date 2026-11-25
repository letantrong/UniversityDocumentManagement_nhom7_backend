using Application.Features.Approvals;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApprovalsController : ControllerBase
{
    private readonly ISender _sender;

    public ApprovalsController(ISender sender)
    {
        _sender = sender;
    }
    [HttpGet("pending")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPendingApprovalsQuery(), cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    [HttpPost("documents/{documentId:guid}/submit")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<IActionResult> SubmitForApproval(
        Guid documentId,
        [FromBody] SubmitApprovalRequest body,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new SubmitDocumentForApprovalCommand(documentId, body.ApproverId, body.StepOrder),
            cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    [HttpPost("{approvalId:guid}/decision")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> SubmitDecision(
        Guid approvalId,
        [FromBody] ApprovalDecisionRequest body,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new SubmitApprovalDecisionCommand(approvalId, body.IsApproved, body.Comments),
            cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    [HttpPost("{approvalId:guid}/approve")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Approve(
        Guid approvalId,
        [FromBody] ApprovalCommentRequest? body,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new SubmitApprovalDecisionCommand(approvalId, true, body?.Comments),
            cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    [HttpPost("{approvalId:guid}/reject")]
    [Authorize(Roles = "Admin,Manager")]    public async Task<IActionResult> Reject(
        Guid approvalId,
        [FromBody] ApprovalCommentRequest? body,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new SubmitApprovalDecisionCommand(approvalId, false, body?.Comments),
            cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
    [HttpPost("documents/{documentId:guid}/archive")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Archive(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ArchiveDocumentCommand(documentId), cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}

public record SubmitApprovalRequest(Guid ApproverId, int StepOrder = 1);
public record ApprovalDecisionRequest(bool IsApproved, string? Comments);
public record ApprovalCommentRequest(string? Comments);