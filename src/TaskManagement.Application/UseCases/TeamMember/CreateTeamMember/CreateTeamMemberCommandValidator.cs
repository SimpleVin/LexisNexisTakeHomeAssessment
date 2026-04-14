using FluentValidation;

namespace TaskManagement.Application.UseCases.TeamMember.CreateTeamMember;

public sealed class CreateTeamMemberCommandValidator : AbstractValidator<CreateTeamMemberCommand>
{
    public CreateTeamMemberCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(320).EmailAddress();
    }
}
