using FluentValidation;
using Maui_Task.Shared.DTOs.Messages;

namespace Maui_Task.Web.Validators
{
    public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
    {
        public SendMessageRequestValidator()
        {
            RuleFor(x => x.ReceiverId)
                .GreaterThan(0).WithMessage("A valid receiver ID is required.");

            RuleFor(x => x.Body)
                .NotEmpty().WithMessage("Message body is required.");
        }
    }
}
