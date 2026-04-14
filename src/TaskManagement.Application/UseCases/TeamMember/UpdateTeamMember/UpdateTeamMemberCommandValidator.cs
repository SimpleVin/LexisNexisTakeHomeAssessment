using FluentValidation;

namespace TaskManagement.Application.UseCases.TeamMember.UpdateTeamMember;

public sealed class UpdateTeamMemberCommandValidator : AbstractValidator<UpdateTeamMemberCommand>
{
    public UpdateTeamMemberCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(320).EmailAddress();
    }
}
