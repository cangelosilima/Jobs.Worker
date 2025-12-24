using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Jobs.Worker.Application.Interfaces;

namespace Jobs.Worker.Infrastructure.Signals;

public sealed class SchedulerSignal : ISchedulerSignal
{
    private readonly Channel<SchedulerSignalType> _channel;

    public SchedulerSignal()
    {
        _channel = Channel.CreateUnbounded<SchedulerSignalType>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ValueTask WaitForSignalAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (timeout <= TimeSpan.Zero)
        {
            DrainSignals();
            return ValueTask.CompletedTask;
        }

        return WaitForSignalInternalAsync(timeout, cancellationToken);
    }

    public void NotifySchedulesChanged()
    {
        _channel.Writer.TryWrite(SchedulerSignalType.SchedulesChanged);
    }

    public void NotifyExecutionsChanged()
    {
        _channel.Writer.TryWrite(SchedulerSignalType.ExecutionsChanged);
    }

    private async ValueTask WaitForSignalInternalAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        try
        {
            if (await _channel.Reader.WaitToReadAsync(timeoutCts.Token))
            {
                DrainSignals();
            }
        }
        catch (OperationCanceledException)
        {
            // timeout or cancellation
        }
    }

    private void DrainSignals()
    {
        while (_channel.Reader.TryRead(out _))
        {
        }
    }

    private enum SchedulerSignalType
    {
        SchedulesChanged,
        ExecutionsChanged
    }
}
