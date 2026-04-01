using FluentValidation;
using Maui_Task.Shared.DTOs.Teams;

namespace Maui_Task.Web.Validators
{
    public class CreateTeamRequestValidator : AbstractValidator<CreateTeamRequest>
    {
        public CreateTeamRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Team name is required.")
                .MaximumLength(150).WithMessage("Team name must not exceed 150 characters.");
        }
    }
}
