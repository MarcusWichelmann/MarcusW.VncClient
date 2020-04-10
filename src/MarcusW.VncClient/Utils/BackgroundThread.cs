using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MarcusW.VncClient.Utils
{
    /// <summary>
    /// Base class for easier creation and clean cancellation of a background thread.
    /// </summary>
    public abstract class BackgroundThread : IDisposable
    {
        private readonly Thread _thread;

        private CancellationTokenSource? _stopCts;

        private bool _started;
        private readonly object _startLock = new object();

        private readonly TaskCompletionSource<object?> _completedTcs = new TaskCompletionSource<object?>();

        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundThread"/>.
        /// </summary>
        /// <param name="name">The thread name.</param>
        protected BackgroundThread(string name)
        {
            _thread = new Thread(ThreadStart) {
                Name = name,
                IsBackground = true
            };
        }

        /// <summary>
        /// Starts the thread.
        /// </summary>
        /// <remarks>
        /// The thread can only be started once.
        /// </remarks>
        /// <param name="cancellationToken">The cancellation token to cancel the thread during startup.</param>
        protected void Start(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BackgroundThread));

            cancellationToken.ThrowIfCancellationRequested();

            lock (_startLock)
            {
                if (_started)
                    throw new InvalidOperationException("Thread already started.");

                // Thread can either be canceled using the token passed to this method or by calling the stop method
                _stopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                _thread.Start(_stopCts.Token);
                _started = true;
            }
        }

        /// <summary>
        /// Stops the thread and waits for completion.
        /// </summary>
        /// <remarks>
        /// It is safe to call this method multiple times.
        /// </remarks>
        protected Task StopAndWaitAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BackgroundThread));

            lock (_startLock)
            {
                if (!_started)
                    throw new InvalidOperationException("Thread has not been started.");
            }

            // stopCts should not be null because _started was true
            Debug.Assert(_stopCts != null, nameof(_stopCts) + " != null");

            // Tell the thread to stop
            _stopCts.Cancel();

            // Wait for completion
            return _completedTcs.Task;
        }

        /// <summary>
        /// Executes the work that should happen in the background.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token that tells the method implementation when to complete.</param>
        protected abstract void ThreadWorker(CancellationToken cancellationToken);

        private void ThreadStart(object? parameter)
        {
            Debug.Assert(parameter != null, nameof(parameter) + " != null");
            var cancellationToken = (CancellationToken)parameter;

            try
            {
                // Do your work...
                ThreadWorker(cancellationToken);
            }
            finally
            {
                // Notify stop method that thread has completed
                _completedTcs.SetResult(null);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                // Ensure the thread is stopped
                _stopCts?.Cancel();
                if (_thread.IsAlive)
                {
                    // Block and wait for completion or hard-kill the thread after 3 seconds
                    if (!_thread.Join(TimeSpan.FromSeconds(3)))
                        _thread.Abort();
                }
            }
            catch
            {
                // Ignore
            }

            // Just to be sure...
            _completedTcs.SetResult(null);

            _stopCts?.Dispose();

            _disposed = true;
        }
    }
}
