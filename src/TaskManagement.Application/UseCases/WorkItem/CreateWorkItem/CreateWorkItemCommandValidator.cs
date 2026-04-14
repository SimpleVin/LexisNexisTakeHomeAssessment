using FluentValidation;

namespace TaskManagement.Application.UseCases.WorkItem.CreateWorkItem;

public sealed class CreateWorkItemCommandValidator : AbstractValidator<CreateWorkItemCommand>
{
    public CreateWorkItemCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
