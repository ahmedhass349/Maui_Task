using System;
using System.Threading.Tasks;
using MimeKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Maui_Task.Web.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string htmlBody);
        Task SendWelcomeEmailAsync(string to, string firstName);
        Task SendTaskReminderEmailAsync(string to, string firstName, string taskTitle, DateTime dueDate, string taskUrl);
        Task SendTaskDueSoonEmailAsync(string to, string firstName, string taskTitle, string timeframe, string taskUrl);
        Task SendTaskOverdueEmailAsync(string to, string firstName, string taskTitle, string taskUrl);
    }
}
