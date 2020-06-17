using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol;
using MarcusW.VncClient.Rendering;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient
{
    /// <summary>
    /// Connection to a remote server using the RFB protocol.
    /// </summary>
    public partial class RfbConnection : INotifyPropertyChanged, IDisposable
    {
        private readonly ILogger<RfbConnection> _logger;

        private readonly object _renderTargetLock = new object();
        private IRenderTarget? _renderTarget;

        private readonly object _connectionStateLock = new object();
        private ConnectionState _connectionState = ConnectionState.Uninitialized;

        private readonly object _startedLock = new object();
        private bool _started;

        private readonly SemaphoreSlim _connectionManagementSemaphore = new SemaphoreSlim(1);

        private readonly CancellationTokenSource _reconnectCts = new CancellationTokenSource();
        private Task? _reconnectTask;

        private bool _disposed;

        /// <summary>
        /// Gets the used RFB protocol implementation.
        /// </summary>
        public IRfbProtocolImplementation ProtocolImplementation { get; }

        /// <summary>
        /// Gets the logger factory implementation that should be used for creating new loggers.
        /// </summary>
        public ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Gets the connect parameters used for establishing this connection.
        /// </summary>
        public ConnectParameters Parameters { get; }

        /// <summary>
        /// Gets or sets the target where received frames should be rendered to.
        /// </summary>
        public IRenderTarget? RenderTarget
        {
            get
            {
                lock (_renderTargetLock)
                    return _renderTarget;
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(RfbConnection));

                // ReSharper disable once InconsistentlySynchronizedField
                LockedSetAndRaiseNotifyWhenChanged(ref _renderTarget, value, _renderTargetLock);
            }
        }

        /// <summary>
        /// Gets the current connection state. Subscribe to <see cref="PropertyChanged"/> to receive change notifications.
        /// </summary>
        public ConnectionState ConnectionState
        {
            get
            {
                lock (_connectionStateLock)
                    return _connectionState;
            }
            private set
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(RfbConnection));

                // ReSharper disable once InconsistentlySynchronizedField
                LockedSetAndRaiseNotifyWhenChanged(ref _connectionState, value, _connectionStateLock);
            }
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        internal RfbConnection(IRfbProtocolImplementation protocolImplementation, ILoggerFactory loggerFactory,
            ConnectParameters parameters)
        {
            ProtocolImplementation = protocolImplementation;
            LoggerFactory = loggerFactory;
            Parameters = parameters;
            RenderTarget = parameters.InitialRenderTarget;

            _logger = loggerFactory.CreateLogger<RfbConnection>();
        }

        internal async Task StartAsync(CancellationToken cancellationToken = default)
        {
            // Synchronize connection management operations.
            await _connectionManagementSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await EstablishNewConnectionAsync(cancellationToken).ConfigureAwait(false);
                ConnectionState = ConnectionState.Connected;
            }
            finally
            {
                _connectionManagementSemaphore.Release();
            }

            lock (_startedLock)
                _started = true;
        }

        /// <summary>
        /// Closes the running remote connection as well as any running reconnect attempts.
        /// </summary>
        /// <remarks>
        /// To cancel the connection establishment, use the <see cref="CancellationToken"/> passed to <see cref="StartAsync"/> instead.
        /// </remarks>
        public async Task CloseAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RfbConnection));

            // This method must not be called before StartAsync finished.
            lock (_startedLock)
            {
                if (!_started)
                    throw new InvalidOperationException(
                        "Connection cannot be closed before the initial connect has completed. "
                        + "Please consider using the cancellation token passed to the connect method instead.");
            }

            // Cancel reconnect attempts.
            // This should release the lock so we can enter it below.
            _reconnectCts.Cancel();
            if (_reconnectTask != null)
            {
                try
                {
                    await _reconnectTask.ConfigureAwait(false);
                }
                catch
                {
                    // Ignored.
                }
            }

            // Synchronize connection management operations.
            await _connectionManagementSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                await CloseConnectionAsync().ConfigureAwait(false);
                ConnectionState = ConnectionState.Closed;
            }
            finally
            {
                _connectionManagementSemaphore.Release();
            }
        }

        // Is called by the other class part to signal us that the running connection has failed in the background.
        private void OnRunningConnectionFailed()
        {
            // Reconnect in the background and store the task away, just for cleanliness.
            _reconnectTask = ReconnectAsync();
        }

        private async Task ReconnectAsync()
        {
            CancellationToken cancellationToken = _reconnectCts.Token;

            ConnectionState = ConnectionState.Interrupted;

            // Synchronize connection management operations.
            await _connectionManagementSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var failedAttempts = 0;
                while (true)
                {
                    // Any attempts remaining?
                    if (Parameters.MaxReconnectAttempts != ConnectParameters.InfiniteReconnects && failedAttempts >= Parameters.MaxReconnectAttempts)
                    {
                        // Giving up.
                        _logger.LogInformation("No reconnect attempts to {endpoint} remaining. Giving up.",
                            Parameters.Endpoint);
                        CleanupPreviousConnection();
                        ConnectionState = ConnectionState.Closed;
                        return;
                    }

                    // Next try
                    try
                    {
                        // Wait before next attempt
                        await Task.Delay(Parameters.ReconnectDelay, cancellationToken).ConfigureAwait(false);

                        // Next try...
                        ConnectionState = ConnectionState.Reconnecting;
                        await EstablishNewConnectionAsync(cancellationToken).ConfigureAwait(false);

                        // This seems to have been successful.
                        ConnectionState = ConnectionState.Connected;
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        // Reconnect was canceled
                        CleanupPreviousConnection();
                        ConnectionState = ConnectionState.Closed;
                        return;
                    }
                    catch (Exception exception)
                    {
                        // Reconnect attempt failed
                        _logger.LogWarning("Reconnect attempt {attempt} to {endpoint} failed.", exception,
                            failedAttempts, Parameters.Endpoint);
                        CleanupPreviousConnection();
                        ConnectionState = ConnectionState.ReconnectFailed;

                        // Next round...
                        failedAttempts++;
                    }
                }
            }
            finally
            {
                _connectionManagementSemaphore.Release();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            // Cancel reconnects
            _reconnectCts.Cancel();

            ConnectionState = ConnectionState.Closed;

            // Acquire the lock to ensure the connection setup is completed before destroying anything.
            // Also ensures no new connections are set up after this point because the lock is never released.
            _connectionManagementSemaphore.Wait();
            try
            {
                // This will do a good enough job in stopping everything, even though we don't call CloseConnectionAsync here.
                CleanupPreviousConnection();
            }
            catch (Exception ex)
            {
                // Should not happen.
                Debug.Fail("Cleaning up previous connection failed unexpectedly.", ex.ToString());
            }

            _reconnectCts.Dispose();
            _connectionManagementSemaphore.Dispose();

            _disposed = true;
        }
    }
}
