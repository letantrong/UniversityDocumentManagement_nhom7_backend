using Application.Features.Documents.Commands.CreateDocument;
using Application.Features.Documents.Commands.UpdateDocument;
using FluentValidation;

namespace Application.Features.Documents.Validators;

public class CreateDocumentCommandValidator : AbstractValidator<CreateDocumentCommand>
{
    public CreateDocumentCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.DepartmentId).NotEmpty();
    }
}

public class UpdateDocumentCommandValidator : AbstractValidator<UpdateDocumentCommand>
{
    public UpdateDocumentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(250);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}
