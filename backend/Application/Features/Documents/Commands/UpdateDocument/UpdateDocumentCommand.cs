using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Documents.Commands.UpdateDocument;

public record UpdateDocumentCommand(
    Guid Id,
    string Title,
    string Description,
    string? Abstract,
    DocumentSecurityLevel SecurityLevel,
    Guid CategoryId,
    Guid? FolderId,
    DocumentStatus? Status) : IRequest<Result>;

public class UpdateDocumentCommandHandler : IRequestHandler<UpdateDocumentCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public UpdateDocumentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result> Handle(UpdateDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == request.Id && !d.IsDeleted, cancellationToken);

        if (document == null)
            return Result.Failure("Document not found.");

        document.Title = request.Title.Trim();
        document.Description = request.Description.Trim();
        document.Abstract = request.Abstract;
        document.SecurityLevel = request.SecurityLevel;
        document.CategoryId = request.CategoryId;
        document.FolderId = request.FolderId;
        if (request.Status.HasValue)
        {
            document.Status = request.Status.Value;
        }

        document.UpdatedAt = _dateTimeService.UtcNow;
        document.UpdatedBy = _currentUserService.Username;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUserService.UserId,
            UserName = _currentUserService.Username,
            Action = AuditAction.Update,
            EntityName = nameof(Document),
            EntityId = document.Id.ToString(),
            NewValues = $"Title: {document.Title}, Status: {document.Status}",
            CreatedAt = _dateTimeService.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success("Document updated successfully.");
    }
}
