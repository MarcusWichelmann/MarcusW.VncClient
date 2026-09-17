using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.EncodingTypes;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing;
using WellKnownEncoding = MarcusW.VncClient.Protocol.EncodingTypes.WellKnownEncodingType;
using MarcusW.VncClient.Protocol.MessageTypes;
using MarcusW.VncClient.Protocol.Services;
using MarcusW.VncClient.Utils;
using Microsoft.Extensions.Logging;

namespace MarcusW.VncClient.Protocol.Implementation.Services.Communication
{
    /// <summary>
    /// A background thread that sends queued messages and provides methods to add messages to the send queue.
    /// </summary>
    public class RfbMessageSender : BackgroundThread, IRfbMessageSender
    {
        private readonly RfbConnectionContext _context;
        private readonly ProtocolState _state;
        private readonly ILogger<RfbMessageSender> _logger;

        private readonly BlockingCollection<QueueItem> _queue = new BlockingCollection<QueueItem>(new ConcurrentQueue<QueueItem>());

        private volatile bool _disposed;

        // Throttling state
        // Using Environment.TickCount64 for monotonic timing (immune to system clock changes)
        private long _lastFramebufferRequestTicks = 0;
        private readonly object _throttleLock = new object();
        // Lock ordering: always acquire _throttleLock before _timerLock to prevent deadlocks
        private readonly HashSet<Timer> _pendingTimers = new HashSet<Timer>();
        private readonly object _timerLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="RfbMessageSender"/>.
        /// </summary>
        /// <param name="context">The connection context.</param>
        public RfbMessageSender(RfbConnectionContext context) : base("RFB Message Sender")
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _state = context.GetState<ProtocolState>();
            _logger = context.Connection.LoggerFactory.CreateLogger<RfbMessageSender>();

            // Log failure events from background thread base
            Failed += (sender, args) => _logger.LogWarning(args.Exception, "Send loop failed.");
        }

        /// <inheritdoc />
        public void StartSendLoop()
        {
            // Removed debug logging for production use
            Start();
        }

        /// <inheritdoc />
        public Task StopSendLoopAsync()
        {
            // Removed debug logging for production use
            return StopAndWaitAsync();
        }

        /// <inheritdoc />
        public void EnqueueInitialMessages(CancellationToken cancellationToken = default)
        {
            // Removed initial message enqueueing debug logging for production use

            // WAYVNC COMPATIBILITY: Use TIER 1-4 encoding set for maximum compatibility
            var minimalEncodingTypes = GetMinimalEncodingTypes();
            var encodingTypesToUse = minimalEncodingTypes.ToImmutableHashSet();
            
            // Removed debug logging for production use

            // Send SetPixelFormat first for maximum compatibility
            _logger.LogDebug("Sending initial SetPixelFormat: {format}.", _state.RemoteFramebufferFormat);
            var setPixelFormatMessage = new Protocol.Implementation.MessageTypes.Outgoing.SetPixelFormatMessage(_state.RemoteFramebufferFormat);
            EnqueueMessage(setPixelFormatMessage, cancellationToken);

            // Send initial SetEncodings
            _logger.LogDebug("Sending initial SetEncodings with {count} encoding types.", encodingTypesToUse.Count);
            var setEncodingsMessage = new SetEncodingsMessage(encodingTypesToUse);
            EnqueueMessage(setEncodingsMessage, cancellationToken);

            // Send a non-incremental (full) framebuffer update request.
            // The first request MUST be non-incremental per the RFB spec, because the server
            // has no baseline for change detection yet. Using incremental:true here can cause
            // the server to respond with 0 rectangles (nothing has changed), which previously
            // broke the update cycle completely. Non-incremental guarantees the server sends
            // the entire framebuffer contents, establishing the baseline for subsequent
            // incremental updates.
            var updateRect = new Rectangle(Position.Origin, _state.RemoteFramebufferSize);
            _logger.LogDebug("Sending initial non-incremental FramebufferUpdateRequest for {rect}.", updateRect);
            var framebufferRequest = new FramebufferUpdateRequestMessage(false, updateRect);
            EnqueueMessage(framebufferRequest, cancellationToken);
            
            // Configurable delay to prevent server flooding (default 200ms, configurable via ConnectParameters)
            var postInitDelay = _context.Connection.Parameters.PostInitializationDelay;
            if (postInitDelay > TimeSpan.Zero)
                Thread.Sleep(postInitDelay);
        }
        
        /// <summary>
        /// Gets encoding types for optimal WayVNC compatibility (TIER 1-4 default, configurable TIER 5)
        /// </summary>
        private IEnumerable<IEncodingType> GetMinimalEncodingTypes()
        {
            // TODO: Add configuration option to enable TIER 5 features for known compatible servers
            const bool enableAdvancedFeatures = false; // Could be configurable in the future
            
            return BuildEncodingTypeSet(includeTier5: enableAdvancedFeatures);
        }


        /// <summary>
        /// Builds an optimized set of encoding types based on tier configuration.
        /// Uses well-known encoding type IDs for type-safe lookups instead of magic strings.
        /// </summary>
        private IEnumerable<IEncodingType> BuildEncodingTypeSet(bool includeTier5 = false)
        {
            var encodingTypes = new List<IEncodingType>();
            
            // TIER 1: Essential encodings (required for basic VNC operation)
            AddEncodingIfExists(encodingTypes, WellKnownEncoding.Raw);        // Required by VNC spec
            AddEncodingIfExists(encodingTypes, WellKnownEncoding.CopyRect);   // Basic, widely supported
            AddEncodingIfExists(encodingTypes, WellKnownEncoding.DesktopSize); // Critical for framebuffer handling
            
            // TIER 2: Common frame encodings (safe compression)
            AddEncodingIfExists(encodingTypes, WellKnownEncoding.ZRLE);       // Efficient compression
            AddEncodingIfExists(encodingTypes, WellKnownEncoding.LastRect);   // End-of-update marker
            
            // TIER 3: Advanced frame encodings
            AddEncodingIfExists(encodingTypes, WellKnownEncoding.ZLib);       // Basic compression
            AddEncodingIfExists(encodingTypes, WellKnownEncoding.Tight);      // Advanced (if TurboJPEG available)
            
            // TIER 4: Quality control pseudo encodings
            AddJpegQualityEncodingsIfExists(encodingTypes);   // Smart JPEG quality control
            
            // TIER 5: Advanced protocol extensions (optional, may cause issues with newer servers)
            if (includeTier5)
            {
                AddEncodingIfExists(encodingTypes, WellKnownEncoding.Fence);              // Advanced synchronization
                AddEncodingIfExists(encodingTypes, WellKnownEncoding.ContinuousUpdates);  // Real-time streaming
                AddEncodingIfExists(encodingTypes, WellKnownEncoding.ExtendedDesktopSize); // Multi-screen support
            }
            
            return encodingTypes;
        }
        
        /// <summary>
        /// Safely adds an encoding type by its well-known ID.
        /// </summary>
        private void AddEncodingIfExists(List<IEncodingType> encodingTypes, WellKnownEncodingType wellKnownType)
        {
            int targetId = (int)wellKnownType;
            var encodingType = _context.SupportedEncodingTypes?.FirstOrDefault(et => et.Id == targetId);
            if (encodingType != null)
            {
                encodingTypes.Add(encodingType);
            }
        }
        
        /// <summary>
        /// Adds JPEG quality control pseudo encodings using well-known ID ranges for type-safe filtering.
        /// </summary>
        private void AddJpegQualityEncodingsIfExists(List<IEncodingType> encodingTypes)
        {
            if (_context.SupportedEncodingTypes == null) return;
            
            // Use well-known ID ranges from the WellKnownEncodingType enum
            const int jpegQualityHigh = (int)WellKnownEncoding.JpegQualityLevelHigh;
            const int jpegQualityLow = (int)WellKnownEncoding.JpegQualityLevelLow;
            const int jpegFineHigh = (int)WellKnownEncoding.JpegFineGrainedQualityLevelHigh;
            const int jpegFineLow = (int)WellKnownEncoding.JpegFineGrainedQualityLevelLow;
            const int jpegSubLow = (int)WellKnownEncoding.JpegSubsamplingLevelLow;
            const int jpegSubHigh = (int)WellKnownEncoding.JpegSubsamplingLevelHigh;
            
            var jpegEncodings = _context.SupportedEncodingTypes
                .Where(et => (et.Id >= jpegQualityLow && et.Id <= jpegQualityHigh) ||
                             (et.Id >= jpegFineLow && et.Id <= jpegFineHigh) ||
                             (et.Id >= jpegSubHigh && et.Id <= jpegSubLow))
                .ToList();
                
            encodingTypes.AddRange(jpegEncodings);
        }

        /// <inheritdoc />
        public void EnqueueMessage<TMessageType>(IOutgoingMessage<TMessageType> message, CancellationToken cancellationToken = default)
            where TMessageType : class, IOutgoingMessageType
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (_disposed)
                throw new ObjectDisposedException(nameof(RfbMessageSender));

            cancellationToken.ThrowIfCancellationRequested();

            TMessageType messageType = GetAndCheckMessageType<TMessageType>();

            // Add message to queue
            try
            {
                _queue.Add(new QueueItem(message, messageType), cancellationToken);
            }
            catch (InvalidOperationException)
            {
                // The queue was marked as complete because the send loop stopped or failed (e.g. the connection broke).
                // Surface this as ObjectDisposedException so callers can treat it like any other "sender is gone" race.
                throw new ObjectDisposedException(nameof(RfbMessageSender), "The send queue has been completed because the send loop stopped or failed.");
            }
        }

        /// <inheritdoc />
        public void EnqueueFramebufferUpdateRequest(Rectangle rectangle, bool incremental, CancellationToken cancellationToken = default)
        {
            var delay = _context.Connection.Parameters.FramebufferUpdateDelay;
            EnqueueFramebufferUpdateRequestDelayed(rectangle, incremental, delay, cancellationToken);
        }

        /// <inheritdoc />
        public void EnqueueFramebufferUpdateRequestDelayed(Rectangle rectangle, bool incremental, TimeSpan delay, CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                _logger.LogDebug("EnqueueFramebufferUpdateRequestDelayed: sender is disposed, skipping.");
                return;
            }

            if (delay <= TimeSpan.Zero)
            {
                // No throttling - enqueue immediately
                _logger.LogDebug("EnqueueFramebufferUpdateRequestDelayed: no delay, enqueueing immediately (incremental={incremental}).", incremental);
                try
                {
                    EnqueueMessage(new FramebufferUpdateRequestMessage(incremental, rectangle), cancellationToken);
                }
                catch (ObjectDisposedException) { _logger.LogDebug("EnqueueFramebufferUpdateRequestDelayed: ObjectDisposedException (connection closed)."); }
                catch (OperationCanceledException) { _logger.LogDebug("EnqueueFramebufferUpdateRequestDelayed: OperationCanceledException."); }
                catch (InvalidOperationException) { _logger.LogWarning("EnqueueFramebufferUpdateRequestDelayed: InvalidOperationException - send queue completed (send loop failed or shutting down)."); }
                return;
            }

            // Calculate actual delay needed based on last request time (using monotonic clock)
            TimeSpan actualDelay;
            lock (_throttleLock)
            {
                long currentTicks = Environment.TickCount64;
                long elapsedMs = currentTicks - _lastFramebufferRequestTicks;
                var elapsed = TimeSpan.FromMilliseconds(elapsedMs);
                actualDelay = elapsed >= delay ? TimeSpan.Zero : delay - elapsed;
                _lastFramebufferRequestTicks = currentTicks + (long)actualDelay.TotalMilliseconds;
            }

            if (actualDelay <= TimeSpan.Zero)
            {
                _logger.LogDebug("EnqueueFramebufferUpdateRequestDelayed: throttle passed, enqueueing now (incremental={incremental}).", incremental);
                try
                {
                    EnqueueMessage(new FramebufferUpdateRequestMessage(incremental, rectangle), cancellationToken);
                }
                catch (ObjectDisposedException) { _logger.LogDebug("EnqueueFramebufferUpdateRequestDelayed: ObjectDisposedException (connection closed)."); }
                catch (OperationCanceledException) { _logger.LogDebug("EnqueueFramebufferUpdateRequestDelayed: OperationCanceledException."); }
                catch (InvalidOperationException) { _logger.LogWarning("EnqueueFramebufferUpdateRequestDelayed: InvalidOperationException - send queue completed."); }
            }
            else
            {
                _logger.LogDebug("EnqueueFramebufferUpdateRequestDelayed: deferring for {delayMs}ms (incremental={incremental}).", actualDelay.TotalMilliseconds, incremental);
                // Use Timer for better lifecycle management than Task.Delay().ContinueWith()
                Timer? timer = null;
                CancellationTokenRegistration? registration = null;

                timer = new Timer(_ =>
                {
                    try
                    {
                        if (!_disposed && !cancellationToken.IsCancellationRequested)
                        {
                            _logger.LogDebug("Throttle timer fired: enqueueing deferred FramebufferUpdateRequest (incremental={incremental}).", incremental);
                            EnqueueMessage(new FramebufferUpdateRequestMessage(incremental, rectangle), cancellationToken);
                        }
                        else
                        {
                            _logger.LogDebug("Throttle timer fired but disposed={disposed}, cancelled={cancelled}.", _disposed, cancellationToken.IsCancellationRequested);
                        }
                    }
                    catch (ObjectDisposedException) { _logger.LogDebug("Throttle timer: ObjectDisposedException (shutdown)."); }
                    catch (OperationCanceledException) { _logger.LogDebug("Throttle timer: OperationCanceledException."); }
                    catch (InvalidOperationException) { _logger.LogWarning("Throttle timer: InvalidOperationException - send queue completed (send loop failed or shutting down)."); }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Unexpected error in throttled framebuffer request callback");
                    }
                    finally
                    {
                        // Clean up timer and registration
                        CleanupThrottleTimer(timer!, registration);
                    }
                }, null, actualDelay, Timeout.InfiniteTimeSpan);

                // Register cancellation callback to clean up timer if cancellation is requested
                if (cancellationToken.CanBeCanceled)
                {
                    registration = cancellationToken.Register(() => CleanupThrottleTimer(timer!, registration));
                }

                lock (_timerLock)
                {
                    if (!_disposed)
                    {
                        _pendingTimers.Add(timer);
                    }
                    else
                    {
                        CleanupThrottleTimer(timer, registration);
                    }
                }
            }
        }

        /// <summary>
        /// Cleans up a throttle timer and its cancellation registration.
        /// </summary>
        private void CleanupThrottleTimer(Timer timer, CancellationTokenRegistration? registration)
        {
            lock (_timerLock)
            {
                _pendingTimers.Remove(timer);
            }
            timer.Dispose();
            registration?.Dispose();
        }

        /// <inheritdoc />
        [Obsolete("Prefer SendMessageAndWaitAsync to avoid sync-over-async blocking. This method exists for backward compatibility.")]
        public void SendMessageAndWait<TMessageType>(IOutgoingMessage<TMessageType> message, CancellationToken cancellationToken = default)
            where TMessageType : class, IOutgoingMessageType
        {
            // Use GetAwaiter().GetResult() instead of .Wait() to avoid AggregateException wrapping.
            // This is still sync-over-async (which can cause deadlocks in certain SynchronizationContexts),
            // but is preferable to .Wait() when a synchronous API must be maintained for backward compatibility.
            SendMessageAndWaitAsync(message, cancellationToken).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public Task SendMessageAndWaitAsync<TMessageType>(IOutgoingMessage<TMessageType> message, CancellationToken cancellationToken = default)
            where TMessageType : class, IOutgoingMessageType
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (_disposed)
                throw new ObjectDisposedException(nameof(RfbMessageSender));

            cancellationToken.ThrowIfCancellationRequested();

            TMessageType messageType = GetAndCheckMessageType<TMessageType>();

            // Create a completion source and ensure that completing the task won't block our send-loop.
            var completionSource = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Add message to queue
            try
            {
                _queue.Add(new QueueItem(message, messageType, completionSource), cancellationToken);
            }
            catch (InvalidOperationException)
            {
                // See EnqueueMessage: the send loop stopped or failed concurrently.
                throw new ObjectDisposedException(nameof(RfbMessageSender), "The send queue has been completed because the send loop stopped or failed.");
            }

            return completionSource.Task;
        }

        // This method will not catch exceptions so the BackgroundThread base class will receive them,
        // raise a "Failure" and trigger a reconnect.
        protected override void ThreadWorker(CancellationToken cancellationToken)
        {
            try
            {
                Debug.Assert(_context.Transport != null, "_context.Transport != null");
                ITransport transport = _context.Transport;

                // Iterate over all queued items (will block if the queue is empty)
                foreach (QueueItem queueItem in _queue.GetConsumingEnumerable(cancellationToken))
                {
                    IOutgoingMessage<IOutgoingMessageType> message = queueItem.Message;
                    IOutgoingMessageType messageType = queueItem.MessageType;

                    _logger.LogDebug("Sending message: {messageName} (id={messageId}).", messageType.Name, messageType.Id);

                    try
                    {
                        // Write message to transport stream
                        messageType.WriteToTransport(message, transport, cancellationToken);
                      
                        _logger.LogDebug("Message sent successfully: {messageName}.", messageType.Name);
                        queueItem.CompletionSource?.SetResult(null);
                      
                        // Configurable delay after SetEncodings to allow server to process encoding changes
                        // Note: FramebufferUpdateRequest throttling is handled by EnqueueFramebufferUpdateRequest
                        if (messageType.Id == (byte)WellKnownOutgoingMessageType.SetEncodings)
                        {
                            var postSetEncodingsDelay = _context.Connection.Parameters.PostSetEncodingsDelay;
                            if (postSetEncodingsDelay > TimeSpan.Zero)
                                Thread.Sleep(postSetEncodingsDelay);
                        }
                    }
                    catch (Exception ex)
                    {
                        // If something went wrong during sending, tell the waiting tasks about it (so for example the GUI doesn't wait forever).
                        queueItem.CompletionSource?.TrySetException(ex);

                        // Send-thread should still fail
                        throw;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Send loop cancelled.");
                SetQueueCancelled();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Send loop failed with exception. Cancelling queue.");
                SetQueueCancelled();
                throw;
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Cancel all pending throttled timers
                lock (_timerLock)
                {
                    foreach (var timer in _pendingTimers)
                    {
                        timer.Dispose();
                    }
                    _pendingTimers.Clear();
                }

                SetQueueCancelled();
                _queue.Dispose();
            }

            _disposed = true;

            base.Dispose(disposing);
        }

        private TMessageType GetAndCheckMessageType<TMessageType>() where TMessageType : class, IOutgoingMessageType
        {
            Debug.Assert(_context.SupportedMessageTypes != null, "_context.SupportedMessageTypes != null");

            TMessageType? messageType = _context.SupportedMessageTypes.OfType<TMessageType>().FirstOrDefault();
            if (messageType == null)
                throw new InvalidOperationException($"Could not find {typeof(TMessageType).Name} in supported message types collection.");

            if (!_state.UsedMessageTypes.Contains(messageType))
                throw new InvalidOperationException($"The message type {messageType.Name} must not be sent before checking for server-side support and marking it as used.");

            return messageType;
        }

        private void SetQueueCancelled()
        {
            _queue.CompleteAdding();
            foreach (QueueItem queueItem in _queue)
                queueItem.CompletionSource?.TrySetCanceled();
        }

        private sealed record QueueItem(
            IOutgoingMessage<IOutgoingMessageType> Message,
            IOutgoingMessageType MessageType,
            TaskCompletionSource<object?>? CompletionSource = null);
    }
}

