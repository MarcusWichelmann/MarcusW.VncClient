using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Output;
using MarcusW.VncClient.Protocol;
using MarcusW.VncClient.Rendering;
using MarcusW.VncClient.Security;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient
{
    /// <summary>
    /// Connection with a remote server using the RFB protocol.
    /// </summary>
    public sealed partial class RfbConnection : INotifyPropertyChanged, IDisposable
    {
        private readonly ILogger<RfbConnection> _logger;

        // For reference-type properties, volatile is sufficient for atomic read/write.
        // The RaiseAndSetIfChanged helper uses Interlocked.CompareExchange for the compare-and-swap pattern.
        private volatile IRenderTarget? _renderTarget;
        private volatile IOutputHandler? _outputHandler;
        private volatile ICursorHandler? _cursorHandler;
        private volatile Exception? _interruptionCause;

        // Stored as int for safe use with Interlocked operations (avoids Unsafe.As<ConnectionState, int>).
        private int _connectionStateValue = (int)ConnectionState.Uninitialized;

        private readonly object _startedLock = new object();
        private bool _started;

        private readonly SemaphoreSlim _connectionManagementSemaphore = new SemaphoreSlim(1);

        private readonly CancellationTokenSource _reconnectCts = new CancellationTokenSource();
        private Task? _reconnectTask;

        private volatile bool _disposed;

        /// <summary>
        /// Tracks the number of reconnect attempts (1-based).
        /// </summary>
        private int _reconnectAttemptCount;

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
        /// Gets the authentication handler that should be used for handshakes on this connection.
        /// Wraps the user-provided handler and caches inputs for reuse on automatic reconnects,
        /// if enabled via <see cref="ConnectParameters.ReuseAuthenticationInputOnReconnect"/>.
        /// </summary>
        /// <remarks>
        /// Created lazily because handshakes only happen after the parameters have been validated,
        /// which guarantees a non-null <see cref="ConnectParameters.AuthenticationHandler"/>.
        /// Lazy initialization is safe here since handshakes are synchronized by the connection management semaphore.
        /// </remarks>
        internal IAuthenticationHandler AuthenticationHandler
            => _cachingAuthenticationHandler ??= new CachingAuthenticationHandler(Parameters.AuthenticationHandler, Parameters.ReuseAuthenticationInputOnReconnect);

        private CachingAuthenticationHandler? _cachingAuthenticationHandler;

        /// <summary>
        /// Gets or sets the target where received frames should be rendered to.
        /// Subscribe to <see cref="PropertyChanged"/> to receive change notifications.
        /// </summary>
        public IRenderTarget? RenderTarget
        {
            get => _renderTarget;
            set
            {
                if (_disposed) throw new ObjectDisposedException(nameof(RfbConnection));
                if (ReferenceEquals(_renderTarget, value)) return;
                _renderTarget = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the handler for output events from the server.
        /// Subscribe to <see cref="PropertyChanged"/> to receive change notifications.
        /// </summary>
        public IOutputHandler? OutputHandler
        {
            get => _outputHandler;
            set
            {
                if (_disposed) throw new ObjectDisposedException(nameof(RfbConnection));
                if (ReferenceEquals(_outputHandler, value)) return;
                _outputHandler = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the handler for cursor updates from the server.
        /// Subscribe to <see cref="PropertyChanged"/> to receive change notifications.
        /// </summary>
        /// <remarks>
        /// Implement <see cref="ICursorHandler"/> to support local cursor rendering,
        /// which significantly improves perceived performance over slow network connections.
        /// </remarks>
        public ICursorHandler? CursorHandler
        {
            get => _cursorHandler;
            set
            {
                if (_disposed) throw new ObjectDisposedException(nameof(RfbConnection));
                if (ReferenceEquals(_cursorHandler, value)) return;
                _cursorHandler = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the <see cref="Exception"/> that caused the last connection interruption.
        /// Subscribe to <see cref="PropertyChanged"/> to receive change notifications.
        /// </summary>
        public Exception? InterruptionCause
        {
            get => _interruptionCause;
            private set
            {
                if (_disposed) throw new ObjectDisposedException(nameof(RfbConnection));
                if (ReferenceEquals(_interruptionCause, value)) return;
                _interruptionCause = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the current connection state. Subscribe to <see cref="PropertyChanged"/> to receive change notifications.
        /// </summary>
        public ConnectionState ConnectionState
        {
            // Volatile.Read provides an atomic read with acquire semantics, which is sufficient
            // for reading the current state. This avoids the previous Unsafe.As<> approach.
            get => (ConnectionState)Volatile.Read(ref _connectionStateValue);
            private set => SetConnectionState(value);
        }

        /// <summary>
        /// Gets the number of reconnect attempts that have been made.
        /// This is 0 for the initial connection and increments with each reconnect attempt.
        /// </summary>
        public int ReconnectAttemptCount => _reconnectAttemptCount;

        /// <summary>
        /// Raised when the connection state changes.
        /// </summary>
        public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        internal RfbConnection(IRfbProtocolImplementation protocolImplementation, ILoggerFactory loggerFactory, ConnectParameters parameters)
        {
            ProtocolImplementation = protocolImplementation;
            LoggerFactory = loggerFactory;
            Parameters = parameters;
            RenderTarget = parameters.InitialRenderTarget;
            OutputHandler = parameters.InitialOutputHandler;

            _logger = loggerFactory.CreateLogger<RfbConnection>();
        }

        internal async Task StartAsync(CancellationToken cancellationToken = default)
        {
            // Synchronize connection management operations.
            await _connectionManagementSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await EstablishNewConnectionAsync(cancellationToken).ConfigureAwait(false);
                SetConnectionState(ConnectionState.Connected, "Initial connection established");
                InterruptionCause = null;
                
                // Reset reconnect counter on initial connection
                _reconnectAttemptCount = 0;
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
                    throw new InvalidOperationException("Connection cannot be closed before the initial connect has completed. "
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
                SetConnectionState(ConnectionState.Closed, "CloseAsync called", isManualAction: true);
            }
            finally
            {
                _connectionManagementSemaphore.Release();
            }
        }

        /// <summary>
        /// Forces a reconnection to the VNC server, even if the current connection is still active.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the reconnection has been initiated.</returns>
        public async Task ForceReconnectAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RfbConnection));

            // This method must not be called before StartAsync finished.
            lock (_startedLock)
            {
                if (!_started)
                    throw new InvalidOperationException("Cannot force reconnect before the initial connect has completed. "
                        + "Please consider using the cancellation token passed to the connect method instead.");
            }

            _logger.LogInformation("Forcing reconnection to {endpoint}...", Parameters.TransportParameters);

            // Cancel any existing reconnect attempts and start a new one.
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

            // Start a new reconnect task.
            _reconnectTask = ReconnectAsync(cancellationToken, isManualAction: true);
        }

        // Is called by the other class part to signal us that the running connection has failed in the background.
        private void OnRunningConnectionFailed(Exception exception)
        {
            // Mark the connection as interrupted (also avoids that this handler gets called twice per connection)
            // Use CompareExchange to ensure we only transition from Connected to Interrupted once
            var previousState = (ConnectionState)Interlocked.CompareExchange(
                ref _connectionStateValue, 
                (int)ConnectionState.Interrupted, 
                (int)ConnectionState.Connected);
            
            if (previousState != ConnectionState.Connected)
                return;

            // Notify property changed
            NotifyPropertyChanged(nameof(ConnectionState));
            
            // Raise the ConnectionStateChanged event
            var args = new ConnectionStateChangedEventArgs(
                ConnectionState.Connected,
                ConnectionState.Interrupted,
                "Connection interrupted due to failure",
                exception,
                _reconnectAttemptCount);
            ConnectionStateChanged?.Invoke(this, args);

            // Remember the interruption cause
            InterruptionCause = exception;

            try
            {
                // Reconnect in the background and store the task away, just for cleanliness.
                _reconnectTask = ReconnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not start reconnect task.");
            }
        }

        private async Task ReconnectAsync(CancellationToken cancellationToken = default, bool isManualAction = false)
        {
            // Always use the internal reconnect cancellation token
            cancellationToken = _reconnectCts.Token;

            // Synchronize connection management operations.
            await _connectionManagementSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Do a cleanup first to ensure that all other non-failed background threads are stopped too.
                CleanupPreviousConnection();

                var failedAttempts = 0;
                while (true)
                {
                    // Any attempts remaining?
                    if (Parameters.MaxReconnectAttempts != ConnectParameters.InfiniteReconnects && failedAttempts >= Parameters.MaxReconnectAttempts)
                    {
                        // Giving up.
                        _logger.LogInformation("No reconnect attempts to {endpoint} remaining. Giving up.", Parameters.TransportParameters);
                        CleanupPreviousConnection();
                        SetConnectionState(ConnectionState.Closed, "Gave up reconnecting after " + failedAttempts + " attempts");
                        return;
                    }

                    // Increment reconnect attempt count
                    Interlocked.Increment(ref _reconnectAttemptCount);
                    var currentAttempt = _reconnectAttemptCount;

                    // Next try
                    try
                    {
                        // Wait before next attempt
                        await Task.Delay(Parameters.ReconnectDelay, cancellationToken).ConfigureAwait(false);

                        // Next try...
                        SetConnectionState(ConnectionState.Reconnecting, $"Reconnect attempt {currentAttempt} in progress", isManualAction: isManualAction);
                        InterruptionCause = null;
                        await EstablishNewConnectionAsync(cancellationToken).ConfigureAwait(false);

                        // This seems to have been successful.
                        SetConnectionState(ConnectionState.Connected, $"Reconnect attempt {currentAttempt} successful", isManualAction: isManualAction);

                        // Reset the reconnect counter after successful connection
                        _reconnectAttemptCount = 0;

                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        // Reconnect was canceled
                        CleanupPreviousConnection();
                        SetConnectionState(ConnectionState.Closed, "Reconnect canceled", isManualAction: isManualAction);
                        return;
                    }
                    catch
                    {
                        // Reconnect attempt failed (exception has already been logged by EstablishNewConnectionAsync)
                        _logger.LogWarning("Reconnect attempt {attempt} to {endpoint} failed.", currentAttempt, Parameters.TransportParameters);
                        CleanupPreviousConnection();
                        SetConnectionState(ConnectionState.ReconnectFailed, $"Reconnect attempt {currentAttempt} failed", isManualAction: isManualAction);

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

            SetConnectionState(ConnectionState.Closed, "Dispose called");

            // Acquire the semaphore to ensure any in-flight connection setup completes before cleanup.
            // The CTS cancellation above should cause reconnect to release the semaphore promptly.
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
            finally
            {
                _connectionManagementSemaphore.Release();
            }

            _reconnectCts.Dispose();
            _connectionManagementSemaphore.Dispose();

            // Mark as disposed AFTER all cleanup is complete.
            // Setting this earlier would cause property setters (InterruptionCause, etc.) to throw
            // ObjectDisposedException while background threads are still shutting down, leading to
            // cascading failures during the reconnect/cleanup path.
            _disposed = true;
        }
    }
}
