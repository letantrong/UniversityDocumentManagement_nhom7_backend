using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Stations;

public record StationDto(Guid Id, string StationCode, string Name, string Location, string? Description, bool IsActive, Guid DepartmentId, string DepartmentName);

public record GetStationsQuery : IRequest<Result<List<StationDto>>>;

public class GetStationsQueryHandler : IRequestHandler<GetStationsQuery, Result<List<StationDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetStationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<StationDto>>> Handle(GetStationsQuery request, CancellationToken cancellationToken)
    {
        var stations = await _context.Stations
            .Include(s => s.Department)
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.StationCode)
            .Select(s => new StationDto(
                s.Id,
                s.StationCode,
                s.Name,
                s.Location,
                s.Description,
                s.IsActive,
                s.DepartmentId,
                s.Department.Name))
            .ToListAsync(cancellationToken);

        return Result<List<StationDto>>.Success(stations);
    }
}

public record CreateStationCommand(string StationCode, string Name, string Location, string? Description, Guid DepartmentId) : IRequest<Result<Guid>>;

public class CreateStationCommandValidator : AbstractValidator<CreateStationCommand>
{
    public CreateStationCommandValidator()
    {
        RuleFor(x => x.StationCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Location).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DepartmentId).NotEmpty();
    }
}

public class CreateStationCommandHandler : IRequestHandler<CreateStationCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public CreateStationCommandHandler(IApplicationDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result<Guid>> Handle(CreateStationCommand request, CancellationToken cancellationToken)
    {
        var exists = await _context.Stations.AnyAsync(s => s.StationCode == request.StationCode && !s.IsDeleted, cancellationToken);
        if (exists)
            return Result<Guid>.Failure($"Station with code '{request.StationCode}' already exists.");

        var station = new Station
        {
            StationCode = request.StationCode.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            Location = request.Location.Trim(),
            Description = request.Description,
            DepartmentId = request.DepartmentId,
            IsActive = true,
            CreatedAt = _dateTimeService.UtcNow
        };

        _context.Stations.Add(station);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(station.Id, "Station created successfully.");
    }
}
