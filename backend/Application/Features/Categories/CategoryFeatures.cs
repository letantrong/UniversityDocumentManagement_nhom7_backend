using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Categories;

public record CategoryDto(Guid Id, string Code, string Name, string Description, int DocumentsCount);

public record GetCategoriesQuery : IRequest<Result<List<CategoryDto>>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, Result<List<CategoryDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetCategoriesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<CategoryDto>>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _context.Categories
            .Include(c => c.Documents)
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Code, c.Name, c.Description, c.Documents.Count(d => !d.IsDeleted)))
            .ToListAsync(cancellationToken);

        return Result<List<CategoryDto>>.Success(categories);
    }
}

public record CreateCategoryCommand(string Code, string Name, string Description) : IRequest<Result<Guid>>;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public CreateCategoryCommandHandler(IApplicationDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var exists = await _context.Categories.AnyAsync(c => c.Code == request.Code && !c.IsDeleted, cancellationToken);
        if (exists)
            return Result<Guid>.Failure($"Category with code '{request.Code}' already exists.");

        var category = new DocumentCategory
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            Description = request.Description,
            CreatedAt = _dateTimeService.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(category.Id, "Category created successfully.");
    }
}
