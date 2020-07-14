using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MarcusW.VncClient.Protocol.Implementation.MessageTypes.Outgoing;
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
            _logger.LogDebug("Starting send loop...");
            Start();
        }

        /// <inheritdoc />
        public Task StopSendLoopAsync()
        {
            _logger.LogDebug("Stopping send loop...");
            return StopAndWaitAsync();
        }

        /// <inheritdoc />
        public void EnqueueInitialMessages(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Enqueuing initial messages...");

            // Send initial SetEncodings
            Debug.Assert(_context.SupportedEncodingTypes != null, "_context.SupportedEncodingTypes != null");
            EnqueueMessage(new SetEncodingsMessage(_context.SupportedEncodingTypes), cancellationToken);

            // Request full framebuffer update
            EnqueueMessage(new FramebufferUpdateRequestMessage(false, new Rectangle(Position.Origin, _state.FramebufferSize)), cancellationToken);
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

            // Add message to queue
            TMessageType messageType = _context.GetMessageType<TMessageType>();
            _queue.Add(new QueueItem(message, messageType), cancellationToken);
        }

        /// <inheritdoc />
        public Task SendMessageAsync<TMessageType>(IOutgoingMessage<TMessageType> message, CancellationToken cancellationToken = default)
            where TMessageType : class, IOutgoingMessageType
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (_disposed)
                throw new ObjectDisposedException(nameof(RfbMessageSender));

            cancellationToken.ThrowIfCancellationRequested();

            // Create a completion source and ensure that completing the task won't block our send-loop.
            var completionSource = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

            TMessageType messageType = _context.GetMessageType<TMessageType>();
            _queue.Add(new QueueItem(message, messageType, completionSource), cancellationToken);

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

                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.LogDebug("Sending {messageName}...", messageType.Name);

                    try
                    {
                        // Write message to transport stream
                        messageType.WriteToTransport(message, transport, cancellationToken);
                        queueItem.CompletionSource?.SetResult(null);
                    }
                    catch (Exception ex)
                    {
                        // If something went wrong during sending, tell the waiting task about it (so for example the GUI doesn't block).
                        queueItem.CompletionSource?.TrySetException(ex);

                        // Send-thread should still fail
                        throw;
                    }
                }
            }
            catch
            {
                // When the loop was canceled or failed, cancel all remaining queue items
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
                SetQueueCancelled();
                _queue.Dispose();
            }

            _disposed = true;

            base.Dispose(disposing);
        }

        private void SetQueueCancelled()
        {
            _queue.CompleteAdding();
            foreach (QueueItem queueItem in _queue)
                queueItem.CompletionSource?.TrySetCanceled();
        }

        private class QueueItem
        {
            public IOutgoingMessage<IOutgoingMessageType> Message { get; }

            public IOutgoingMessageType MessageType { get; }

            public TaskCompletionSource<object?>? CompletionSource { get; }

            public QueueItem(IOutgoingMessage<IOutgoingMessageType> message, IOutgoingMessageType messageType, TaskCompletionSource<object?>? completionSource = null)
            {
                Message = message;
                MessageType = messageType;
                CompletionSource = completionSource;
            }
        }
    }
}
