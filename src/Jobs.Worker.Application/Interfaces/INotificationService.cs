using System.Threading;
using System.Threading.Tasks;
using Jobs.Worker.Domain.Entities;
using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Application.Interfaces;

public interface INotificationService
{
    Task SendNotificationAsync(JobExecution execution, NotificationTrigger trigger, CancellationToken cancellationToken = default);
    Task SendEmailAsync(string[] recipients, string subject, string body, CancellationToken cancellationToken = default);
    Task SendTeamsNotificationAsync(string webhookUrl, string title, string message, string? facts = null, CancellationToken cancellationToken = default);
    Task SendSlackNotificationAsync(string webhookUrl, string message, CancellationToken cancellationToken = default);
    Task SendWebhookAsync(string url, object payload, CancellationToken cancellationToken = default);
}
