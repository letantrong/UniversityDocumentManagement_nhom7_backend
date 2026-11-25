using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Documents.Commands.DeleteDocument;

public record DeleteDocumentCommand(Guid Id) : IRequest<Result>;

public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;

    public DeleteDocumentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == request.Id && !d.IsDeleted, cancellationToken);

        if (document == null)
            return Result.Failure("Document not found.");

        document.IsDeleted = true;
        document.DeletedAt = _dateTimeService.UtcNow;
        document.DeletedBy = _currentUserService.Username;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUserService.UserId,
            UserName = _currentUserService.Username,
            Action = AuditAction.Delete,
            EntityName = nameof(Document),
            EntityId = document.Id.ToString(),
            CreatedAt = _dateTimeService.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success("Document deleted successfully.");
    }
}
