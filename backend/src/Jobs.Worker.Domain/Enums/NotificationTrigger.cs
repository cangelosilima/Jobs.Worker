using System;

namespace Jobs.Worker.Domain.Enums;

[Flags]
public enum NotificationTrigger
{
    None = 0,
    OnStart = 1,
    OnRetry = 2,
    OnSuccess = 4,
    OnFailure = 8,
    OnTimeout = 16,
    OnSkip = 32,
    OnCancel = 64,
    All = OnStart | OnRetry | OnSuccess | OnFailure | OnTimeout | OnSkip | OnCancel
}
