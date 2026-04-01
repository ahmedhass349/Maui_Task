// FILE: Validators/UpdateProfileRequestValidator.cs
// STATUS: UPDATED
// CHANGES: Replaced FullName rule with FirstName/LastName rules (#24)

using FluentValidation;
using Maui_Task.Shared.DTOs.Settings;

namespace Maui_Task.Web.Validators
{
    public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
    {
        public UpdateProfileRequestValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(50).WithMessage("First name must not exceed 50 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(50).WithMessage("Last name must not exceed 50 characters.");

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("A valid email address is required.")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.AvatarUrl)
                .MaximumLength(2048).WithMessage("Avatar URL must not exceed 2048 characters.")
                .When(x => !string.IsNullOrEmpty(x.AvatarUrl));

            RuleFor(x => x.Company)
                .MaximumLength(200).WithMessage("Company must not exceed 200 characters.")
                .When(x => x.Company != null);

            RuleFor(x => x.Country)
                .MaximumLength(100).WithMessage("Country must not exceed 100 characters.")
                .When(x => x.Country != null);

            RuleFor(x => x.Phone)
                .MaximumLength(30).WithMessage("Phone must not exceed 30 characters.")
                .When(x => x.Phone != null);

            RuleFor(x => x.Timezone)
                .MaximumLength(100).WithMessage("Timezone must not exceed 100 characters.")
                .When(x => x.Timezone != null);
        }
    }
}
