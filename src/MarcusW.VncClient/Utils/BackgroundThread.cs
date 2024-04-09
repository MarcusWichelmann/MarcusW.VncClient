using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusW.VncClient.Utils
{
    /// <summary>
    /// Base class for easier creation and clean cancellation of a background thread.
    /// </summary>
    public abstract class BackgroundThread : IBackgroundThread
    {
        private readonly CancellationTokenSource _stopCts = new CancellationTokenSource();
        private Lazy<Task> _task;

        private volatile bool _disposed;

        /// <inheritdoc />
        public event EventHandler<BackgroundThreadFailedEventArgs>? Failed;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundThread"/>.
        /// </summary>
        /// <param name="name">The thread name.</param>
        [Obsolete("The name field is no longer used")]
        protected BackgroundThread(string name)
            : this()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundThread"/>.
        /// </summary>
        protected BackgroundThread()
        {
            _task = new Lazy<Task>(() => ThreadWorker(_stopCts.Token));
        }

        /// <summary>
        /// Starts the thread.
        /// </summary>
        /// <remarks>
        /// The thread can only be started once.
        /// </remarks>
        protected void Start()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BackgroundThread));

            try
            {
                // Do your work...
                _ = _task.Value;
            }
            catch (Exception exception) when (!(exception is OperationCanceledException || exception is ThreadAbortException))
            {
                Failed?.Invoke(this, new BackgroundThreadFailedEventArgs(exception));
            }
        }

        /// <summary>
        /// Stops the thread and waits for completion.
        /// </summary>
        /// <remarks>
        /// It is safe to call this method multiple times.
        /// </remarks>
        protected async Task StopAndWaitAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BackgroundThread));

            // Tell the thread to stop
            _stopCts.Cancel();

            // Wait for completion
            if (_task.IsValueCreated)
            {
                await _task.Value.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Executes the work that should happen in the background.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token that tells the method implementation when to complete.</param>
        protected abstract Task ThreadWorker(CancellationToken cancellationToken);

        /// <inheritdoc />
        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _stopCts.Cancel();
                _stopCts.Dispose();
            }

            _disposed = true;
        }
    }
}
