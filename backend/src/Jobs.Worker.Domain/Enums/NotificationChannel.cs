using System;

namespace Jobs.Worker.Domain.Enums;

[Flags]
public enum NotificationChannel
{
    None = 0,
    Email = 1,
    Teams = 2,
    Slack = 4,
    Webhook = 8
}
