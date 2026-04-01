using FluentValidation;
using Maui_Task.Shared.DTOs.Calendar;

namespace Maui_Task.Web.Validators
{
    public class CreateCalendarEventRequestValidator : AbstractValidator<CreateCalendarEventRequest>
    {
        public CreateCalendarEventRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

            RuleFor(x => x.StartAt)
                .NotEmpty().WithMessage("Start date/time is required.");

            RuleFor(x => x.EndAt)
                .NotEmpty().WithMessage("End date/time is required.")
                .GreaterThan(x => x.StartAt).WithMessage("End date/time must be after start date/time.");

            RuleFor(x => x.Color)
                .MaximumLength(20).WithMessage("Color must not exceed 20 characters.")
                .Matches(@"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$")
                .WithMessage("Color must be a valid hex color (e.g. #FFF, #FF5733).")
                .When(x => !string.IsNullOrEmpty(x.Color));

            RuleFor(x => x.MeetingLink)
                .MaximumLength(2048).WithMessage("Meeting link must not exceed 2048 characters.")
                .When(x => !string.IsNullOrEmpty(x.MeetingLink));
        }
    }
}
