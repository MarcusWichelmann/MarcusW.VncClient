using System;

namespace MarcusW.VncClient.Protocol.Implementation
{
    /// <summary>
    /// The flags sent with a ServerFence or ClientFence message.
    /// </summary>
    [Flags]
    public enum FenceFlags : uint
    {
        /// <summary>
        /// All messages preceding this one must have finished processing and taken effect before the response is sent.
        /// Messages following this one are unaffected and may be processed in any order the protocol permits, even before the response is sent.
        /// </summary>
        BlockBefore = 0b1,

        /// <summary>
        /// All messages following this one must not start processing until the response is sent.
        /// Messages preceding this one are unaffected and may be processed in any order the protocol permits, even being delayed until after the response is sent.
        /// </summary>
        BlockAfter = 0b10,

        /// <summary>
        /// The message following this one must be executed in an atomic manner so that anything preceding the fence response must not be affected by the message,
        /// and anything following the fence response must be affected by the message.
        /// Anything unaffected by the following message can be sent at any time the protocol permits.
        /// </summary>
        SyncNext = 0b100,

        /// <summary>
        /// Indicates that this is a new request and that a response is expected. If this bit is cleared then this message is a response to an earlier request.
        /// </summary>
        Request = (uint)0b_10000000_00000000_00000000_00000000,

        /// <summary>
        /// A bit mask for all supported flags.
        /// </summary>
        SupportedFlagsMask = BlockBefore | BlockAfter | SyncNext | Request
    }
}
