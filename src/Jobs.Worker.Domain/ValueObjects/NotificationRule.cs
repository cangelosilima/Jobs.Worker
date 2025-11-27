using Jobs.Worker.Domain.Enums;

namespace Jobs.Worker.Domain.ValueObjects;

public class NotificationRule
{
    public NotificationChannel Channels { get; private set; }
    public NotificationTrigger Triggers { get; private set; }
    public List<string> EmailRecipients { get; private set; } = new();
    public string? TeamsWebhookUrl { get; private set; }
    public string? SlackWebhookUrl { get; private set; }
    public string? CustomWebhookUrl { get; private set; }

    private NotificationRule() { }

    public NotificationRule(
        NotificationChannel channels,
        NotificationTrigger triggers,
        List<string>? emailRecipients = null,
        string? teamsWebhookUrl = null,
        string? slackWebhookUrl = null,
        string? customWebhookUrl = null)
    {
        Channels = channels;
        Triggers = triggers;
        EmailRecipients = emailRecipients ?? new List<string>();
        TeamsWebhookUrl = teamsWebhookUrl;
        SlackWebhookUrl = slackWebhookUrl;
        CustomWebhookUrl = customWebhookUrl;
    }

    public bool ShouldNotify(NotificationTrigger trigger, NotificationChannel channel)
    {
        return Triggers.HasFlag(trigger) && Channels.HasFlag(channel);
    }
}
