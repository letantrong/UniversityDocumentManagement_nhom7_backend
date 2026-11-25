using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Folders;

public record FolderDto(Guid Id, string Name, string Description, Guid DepartmentId, string DepartmentName, Guid? ParentFolderId, int DocumentsCount);

public record GetFoldersQuery(Guid? DepartmentId = null) : IRequest<Result<List<FolderDto>>>;

public class GetFoldersQueryHandler : IRequestHandler<GetFoldersQuery, Result<List<FolderDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetFoldersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<FolderDto>>> Handle(GetFoldersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Folders
            .Include(f => f.Department)
            .Include(f => f.Documents)
            .Where(f => !f.IsDeleted);

        if (request.DepartmentId.HasValue)
        {
            query = query.Where(f => f.DepartmentId == request.DepartmentId.Value);
        }

        var folders = await query
            .OrderBy(f => f.Name)
            .Select(f => new FolderDto(
                f.Id,
                f.Name,
                f.Description,
                f.DepartmentId,
                f.Department.Name,
                f.ParentFolderId,
                f.Documents.Count(d => !d.IsDeleted)))
            .ToListAsync(cancellationToken);

        return Result<List<FolderDto>>.Success(folders);
    }
}

public record CreateFolderCommand(string Name, string Description, Guid DepartmentId, Guid? ParentFolderId) : IRequest<Result<Guid>>;

public class CreateFolderCommandValidator : AbstractValidator<CreateFolderCommand>
{
    public CreateFolderCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DepartmentId).NotEmpty();
    }
}

public class CreateFolderCommandHandler : IRequestHandler<CreateFolderCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public CreateFolderCommandHandler(IApplicationDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result<Guid>> Handle(CreateFolderCommand request, CancellationToken cancellationToken)
    {
        var folder = new Folder
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            DepartmentId = request.DepartmentId,
            ParentFolderId = request.ParentFolderId,
            CreatedAt = _dateTimeService.UtcNow
        };

        _context.Folders.Add(folder);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(folder.Id, "Folder created successfully.");
    }
}
