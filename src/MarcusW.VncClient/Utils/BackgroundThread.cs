using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace MarcusW.VncClient.Utils;

/// <summary>
///     Base class for easier creation and clean cancellation of a background thread.
/// </summary>
[PublicAPI]
public abstract class BackgroundThread : IBackgroundThread
{
    private readonly object _lock = new();
    private readonly CancellationTokenSource _stopCts = new();

    private volatile bool _disposed;
    private Task? _task;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BackgroundThread" />.
    /// </summary>
    /// <param name="name">The thread name.</param>
    [Obsolete("The name field is no longer used")]
    protected BackgroundThread(string name) : this() { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BackgroundThread" />.
    /// </summary>
    protected BackgroundThread() { }

    /// <inheritdoc />
    public event EventHandler<BackgroundThreadFailedEventArgs>? Failed;

    /// <inheritdoc />
    public void Dispose() => Dispose(true);

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _stopCts.Cancel();
            _stopCts.Dispose();
        }

        _disposed = true;
    }

    /// <summary>
    ///     Starts the thread.
    /// </summary>
    /// <remarks>
    ///     The thread can only be started once.
    /// </remarks>
    protected void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(BackgroundThread));

        // Do your work...
        try
        {
            lock (_lock)
                _task ??= ThreadWorker(_stopCts.Token);
        }
        catch (Exception exception) when (exception is not (OperationCanceledException or ThreadAbortException))
        {
            Failed?.Invoke(this, new BackgroundThreadFailedEventArgs(exception));
        }
    }

    /// <summary>
    ///     Stops the thread and waits for completion.
    /// </summary>
    /// <remarks>
    ///     It is safe to call this method multiple times.
    /// </remarks>
    protected async Task StopAndWaitAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(BackgroundThread));

        // Tell the thread to stop
        await _stopCts.CancelAsync();

        // Wait for completion
        if (_task is not null)
        {
            try
            {
                await _task.ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not (OperationCanceledException or ThreadAbortException))
            {
                Failed?.Invoke(this, new BackgroundThreadFailedEventArgs(exception));
            }
        }
    }

    /// <summary>
    ///     Executes the work that should happen in the background.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token that tells the method implementation when to complete.</param>
    protected abstract Task ThreadWorker(CancellationToken cancellationToken);
}
