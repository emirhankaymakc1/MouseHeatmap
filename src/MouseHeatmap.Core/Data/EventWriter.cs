using System.Threading.Channels;
using MouseHeatmap.Core.Models;

namespace MouseHeatmap.Core.Data;

public sealed class EventWriter : IAsyncDisposable
{
    private const int BatchSize = 200;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(2);

    private readonly EventRepository _repository;
    private readonly Channel<MouseEvent> _channel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _pumpTask;
    private long _pendingCount;

    public long TotalWritten { get; private set; }

    public int PendingCount => (int)Interlocked.Read(ref _pendingCount);

    public EventWriter(EventRepository repository)
    {
        _repository = repository;
        _channel = Channel.CreateUnbounded<MouseEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        _pumpTask = Task.Run(PumpAsync);
    }

    public void Enqueue(MouseEvent mouseEvent)
    {
        if (_channel.Writer.TryWrite(mouseEvent))
            Interlocked.Increment(ref _pendingCount);
    }

    private async Task PumpAsync()
    {
        var buffer = new List<MouseEvent>(BatchSize);
        var reader = _channel.Reader;

        while (!_cts.IsCancellationRequested)
        {
            try
            {
                var waitTask = reader.WaitToReadAsync(_cts.Token).AsTask();
                var completed = await Task.WhenAny(waitTask, Task.Delay(FlushInterval));
                if (completed == waitTask && !await waitTask)
                    break;

                while (buffer.Count < BatchSize && reader.TryRead(out var e))
                    buffer.Add(e);

                Flush(buffer);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        while (reader.TryRead(out var e))
            buffer.Add(e);
        Flush(buffer);
    }

    private void Flush(List<MouseEvent> buffer)
    {
        if (buffer.Count == 0) return;
        try
        {
            _repository.InsertBatch(buffer);
            TotalWritten += buffer.Count;
        }
        catch (Exception)
        {
        }
        finally
        {
            Interlocked.Add(ref _pendingCount, -buffer.Count);
        }
        buffer.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        _cts.Cancel();
        await _pumpTask.ConfigureAwait(false);
        _cts.Dispose();
    }
}
