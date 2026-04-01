// FILE: Validators/UpdateStatusRequestValidator.cs
// STATUS: NEW
// CHANGES: Created for extracted UpdateStatusRequest DTO (#5)

using FluentValidation;
using Maui_Task.Shared.DTOs.Tasks;

namespace Maui_Task.Web.Validators
{
    public class UpdateStatusRequestValidator : AbstractValidator<UpdateStatusRequest>
    {
        public UpdateStatusRequestValidator()
        {
            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status is required.")
                .Must(s => s == "Todo" || s == "InProgress" || s == "Review" || s == "Completed")
                .WithMessage("Status must be one of: Todo, InProgress, Review, Completed.");
        }
    }
}
