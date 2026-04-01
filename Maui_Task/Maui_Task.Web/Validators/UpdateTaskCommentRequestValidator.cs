// FILE: Validators/UpdateTaskCommentRequestValidator.cs
// STATUS: NEW
// CHANGES: Created for fully exposing TaskComment entity (#21)

using FluentValidation;
using Maui_Task.Shared.DTOs.TaskComments;

namespace Maui_Task.Web.Validators
{
    public class UpdateTaskCommentRequestValidator : AbstractValidator<UpdateTaskCommentRequest>
    {
        public UpdateTaskCommentRequestValidator()
        {
            RuleFor(x => x.Body)
                .NotEmpty().WithMessage("Comment body is required.")
                .MaximumLength(2000).WithMessage("Comment body must not exceed 2000 characters.");
        }
    }
}
