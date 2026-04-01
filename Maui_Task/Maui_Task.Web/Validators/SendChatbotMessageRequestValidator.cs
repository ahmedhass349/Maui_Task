using FluentValidation;
using Maui_Task.Shared.DTOs.Chatbot;

namespace Maui_Task.Web.Validators
{
    public class SendChatbotMessageRequestValidator : AbstractValidator<SendChatbotMessageRequest>
    {
        public SendChatbotMessageRequestValidator()
        {
            RuleFor(x => x.Text)
                .NotEmpty().WithMessage("Message text is required.");
        }
    }
}
