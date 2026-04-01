using FluentValidation;
using Maui_Task.Shared.DTOs.Auth;

namespace Maui_Task.Web.Validators
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Password confirmation is required.")
                .Equal(x => x.Password).WithMessage("Passwords do not match.");

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
