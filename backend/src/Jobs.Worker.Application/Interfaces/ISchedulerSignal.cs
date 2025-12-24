using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobs.Worker.Application.Interfaces;

public interface ISchedulerSignal
{
    ValueTask WaitForSignalAsync(TimeSpan timeout, CancellationToken cancellationToken);
    void NotifySchedulesChanged();
    void NotifyExecutionsChanged();
}
