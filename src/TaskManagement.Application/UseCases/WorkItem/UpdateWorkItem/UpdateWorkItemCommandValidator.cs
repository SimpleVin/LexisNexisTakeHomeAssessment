using FluentValidation;

namespace TaskManagement.Application.UseCases.WorkItem.UpdateWorkItem;

public sealed class UpdateWorkItemCommandValidator : AbstractValidator<UpdateWorkItemCommand>
{
    public UpdateWorkItemCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
