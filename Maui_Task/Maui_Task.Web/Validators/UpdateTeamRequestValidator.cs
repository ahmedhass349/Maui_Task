// FILE: Validators/UpdateTeamRequestValidator.cs
// STATUS: NEW
// CHANGES: Created for missing UpdateTeam endpoint (#22)

using FluentValidation;
using Maui_Task.Shared.DTOs.Teams;

namespace Maui_Task.Web.Validators
{
    public class UpdateTeamRequestValidator : AbstractValidator<UpdateTeamRequest>
    {
        public UpdateTeamRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Team name is required.")
                .MaximumLength(150).WithMessage("Team name must not exceed 150 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
                .When(x => x.Description != null);
        }
    }
}
