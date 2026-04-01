using FluentValidation;
using Maui_Task.Shared.DTOs.Projects;

namespace Maui_Task.Web.Validators
{
    public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
    {
        public CreateProjectRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Project name is required.")
                .MaximumLength(150).WithMessage("Project name must not exceed 150 characters.");

            RuleFor(x => x.Color)
                .MaximumLength(20).WithMessage("Color must not exceed 20 characters.")
                .Matches(@"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$")
                .WithMessage("Color must be a valid hex color (e.g. #FFF, #FF5733).")
                .When(x => !string.IsNullOrEmpty(x.Color));
        }
    }
}
