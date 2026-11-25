using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Departments;

public record DepartmentDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    string? Email,
    string? PhoneNumber,
    Guid? ParentDepartmentId,
    string? ParentDepartmentName,
    int UsersCount,
    int DocumentsCount);

public record GetDepartmentsQuery : IRequest<Result<List<DepartmentDto>>>;

public class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, Result<List<DepartmentDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetDepartmentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<DepartmentDto>>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var departments = await _context.Departments
            .Include(d => d.ParentDepartment)
            .Include(d => d.Users)
            .Include(d => d.Documents)
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(
                d.Id,
                d.Code,
                d.Name,
                d.Description,
                d.Email,
                d.PhoneNumber,
                d.ParentDepartmentId,
                d.ParentDepartment != null ? d.ParentDepartment.Name : null,
                d.Users.Count(u => !u.IsDeleted),
                d.Documents.Count(doc => !doc.IsDeleted)))
            .ToListAsync(cancellationToken);

        return Result<List<DepartmentDto>>.Success(departments);
    }
}

public record CreateDepartmentCommand(
    string Code,
    string Name,
    string Description,
    string? Email,
    string? PhoneNumber,
    Guid? ParentDepartmentId) : IRequest<Result<Guid>>;

public class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}

public class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public CreateDepartmentCommandHandler(IApplicationDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result<Guid>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var exists = await _context.Departments.AnyAsync(d => d.Code == request.Code && !d.IsDeleted, cancellationToken);
        if (exists)
            return Result<Guid>.Failure($"Department with code '{request.Code}' already exists.");

        var department = new Department
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            Description = request.Description,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            ParentDepartmentId = request.ParentDepartmentId,
            CreatedAt = _dateTimeService.UtcNow
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(department.Id, "Department created successfully.");
    }
}
