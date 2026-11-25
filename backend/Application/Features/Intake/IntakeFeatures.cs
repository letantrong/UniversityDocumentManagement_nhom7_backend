using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Intake;

public record IntakeRecordDto(
    Guid Id,
    string IntakeNumber,
    string SenderName,
    string? SenderOrganization,
    string? SenderContact,
    DateTime ReceivedAt,
    IntakeStatus Status,
    string StatusName,
    string? Notes,
    Guid StationId,
    string StationName,
    Guid? HandlerId,
    string? HandlerName,
    Guid? DocumentId);

public record GetIntakesQuery(PaginationParams Params, IntakeStatus? Status = null) : IRequest<Result<PaginatedList<IntakeRecordDto>>>;

public class GetIntakesQueryHandler : IRequestHandler<GetIntakesQuery, Result<PaginatedList<IntakeRecordDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetIntakesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<IntakeRecordDto>>> Handle(GetIntakesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.IntakeRecords
            .Include(i => i.Station)
            .Include(i => i.Handler)
            .Where(i => !i.IsDeleted)
            .AsNoTracking();

        if (request.Status.HasValue)
            query = query.Where(i => i.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.Params.SearchTerm))
        {
            var term = request.Params.SearchTerm.Trim().ToLower();
            query = query.Where(i => i.IntakeNumber.ToLower().Contains(term) ||
                                     i.SenderName.ToLower().Contains(term) ||
                                     (i.SenderOrganization != null && i.SenderOrganization.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(i => i.ReceivedAt)
            .Skip((request.Params.PageNumber - 1) * request.Params.PageSize)
            .Take(request.Params.PageSize)
            .Select(i => new IntakeRecordDto(
                i.Id,
                i.IntakeNumber,
                i.SenderName,
                i.SenderOrganization,
                i.SenderContact,
                i.ReceivedAt,
                i.Status,
                i.Status.ToString(),
                i.Notes,
                i.StationId,
                i.Station.Name,
                i.HandlerId,
                i.Handler != null ? i.Handler.FullName : null,
                i.DocumentId))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<IntakeRecordDto>>.Success(
            new PaginatedList<IntakeRecordDto>(items, totalCount, request.Params.PageNumber, request.Params.PageSize));
    }
}

public record CreateIntakeCommand(
    string SenderName,
    string? SenderOrganization,
    string? SenderContact,
    Guid StationId,
    string? Notes) : IRequest<Result<Guid>>;

public class CreateIntakeCommandValidator : AbstractValidator<CreateIntakeCommand>
{
    public CreateIntakeCommandValidator()
    {
        RuleFor(x => x.SenderName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.StationId).NotEmpty();
    }
}

public class CreateIntakeCommandHandler : IRequestHandler<CreateIntakeCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeService _dateTimeService;
    private readonly INotificationService _notificationService;

    public CreateIntakeCommandHandler(
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

    public async Task<Result<Guid>> Handle(CreateIntakeCommand request, CancellationToken cancellationToken)
    {
        var station = await _context.Stations
            .FirstOrDefaultAsync(s => s.Id == request.StationId && !s.IsDeleted, cancellationToken);

        if (station == null)
            return Result<Guid>.Failure("Station not found.");

        var year = _dateTimeService.UtcNow.Year;
        var count = await _context.IntakeRecords.CountAsync(i => i.ReceivedAt.Year == year, cancellationToken);
        var intakeNumber = $"INT-{year}-{(count + 1):D5}";

        var intake = new IntakeRecord
        {
            IntakeNumber = intakeNumber,
            SenderName = request.SenderName.Trim(),
            SenderOrganization = request.SenderOrganization,
            SenderContact = request.SenderContact,
            StationId = request.StationId,
            HandlerId = _currentUserService.UserId,
            ReceivedAt = _dateTimeService.UtcNow,
            Status = IntakeStatus.Received,
            Notes = request.Notes,
            CreatedAt = _dateTimeService.UtcNow
        };

        _context.IntakeRecords.Add(intake);
        await _context.SaveChangesAsync(cancellationToken);

        await _notificationService.BroadcastAsync(
            $"New Document Intake",
            $"New intake record {intake.IntakeNumber} received at {station.Name}.",
            cancellationToken);

        return Result<Guid>.Success(intake.Id, "Intake record registered successfully.");
    }
}
