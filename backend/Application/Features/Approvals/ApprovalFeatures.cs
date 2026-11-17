using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Approvals;

public record ApprovalDto(
    Guid Id,
    Guid DocumentId,
    string DocumentTitle,
    string DocumentNumber,
    Guid ApproverId,
    string ApproverName,
    int StepOrder,
    ApprovalStatus Status,
    string StatusName,
    string? Comments,
    DateTime? DecisionAt,
    DateTime CreatedAt);

public record GetPendingApprovalsQuery : IRequest<Result<List<ApprovalDto>>>;

public class GetPendingApprovalsQueryHandler : IRequestHandler<GetPendingApprovalsQuery, Result<List<ApprovalDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetPendingApprovalsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<List<ApprovalDto>>> Handle(GetPendingApprovalsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;

        var query = _context.Approvals
            .Include(a => a.Document)
            .Include(a => a.Approver)
            .Where(a => !a.IsDeleted && a.Status == ApprovalStatus.Pending);

        if (currentUserId.HasValue)
        {
            query = query.Where(a => a.ApproverId == currentUserId.Value);
        }

        var approvals = await query
            .OrderBy(a => a.CreatedAt)
            .Select(a => new ApprovalDto(
                a.Id,
                a.DocumentId,
                a.Document.Title,
                a.Document.DocumentNumber,
                a.ApproverId,
                a.Approver.FullName,
                a.StepOrder,
                a.Status,
                a.Status.ToString(),
                a.Comments,
                a.DecisionAt,
                a.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<List<ApprovalDto>>.Success(approvals);
    }
}

public record SubmitApprovalDecisionCommand(
    Guid ApprovalId,
    bool IsApproved,
    string? Comments) : IRequest<Result>;

public class SubmitApprovalDecisionCommandValidator : AbstractValidator<SubmitApprovalDecisionCommand>
{
    public SubmitApprovalDecisionCommandValidator()
    {
        RuleFor(x => x.ApprovalId).NotEmpty();
    }
}

public class SubmitApprovalDecisionCommandHandler : IRequestHandler<SubmitApprovalDecisionCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly INotificationService _notificationService;

    public SubmitApprovalDecisionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService,
        INotificationService notificationService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _notificationService = notificationService;
    }

    public async Task<Result> Handle(SubmitApprovalDecisionCommand request, CancellationToken cancellationToken)
    {
        var approval = await _context.Approvals
            .Include(a => a.Document)
            .FirstOrDefaultAsync(a => a.Id == request.ApprovalId && !a.IsDeleted, cancellationToken);

        if (approval == null)
            return Result.Failure("Approval request not found.");

        if (approval.Status != ApprovalStatus.Pending)
            return Result.Failure("Approval request has already been processed.");

        approval.Status = request.IsApproved ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
        approval.Comments = request.Comments;
        approval.DecisionAt = _dateTimeService.UtcNow;

        var document = approval.Document;
        document.Status = request.IsApproved ? DocumentStatus.Approved : DocumentStatus.Rejected;
        document.UpdatedAt = _dateTimeService.UtcNow;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUserService.UserId,
            UserName = _currentUserService.Username,
            Action = request.IsApproved ? AuditAction.Approve : AuditAction.Reject,
            EntityName = nameof(Approval),
            EntityId = approval.Id.ToString(),
            NewValues = $"Document: {document.DocumentNumber}, Decision: {approval.Status}",
            CreatedAt = _dateTimeService.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        var decisionWord = request.IsApproved ? "Approved" : "Rejected";
        await _notificationService.SendToUserAsync(
            document.OwnerId,
            $"Document {decisionWord}",
            $"Your document '{document.Title}' has been {decisionWord.ToLower()} by {_currentUserService.Username}.",
            document.Id.ToString(),
            cancellationToken);

        return Result.Success($"Document {decisionWord.ToLower()} successfully.");
    }
}
