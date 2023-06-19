using SuperNet.Netcode.Util;

namespace SuperNet.Netcode.Transport {

	/// <summary>
	/// Implements a message that can be sent by the netcode to a connected peer.
	/// </summary>
	public interface IMessage : IWritable {

		/// <summary>
		/// Timestamp of this message.
		/// <para>If message is not timed, this is ignored.</para>
		/// </summary>
		HostTimestamp Timestamp { get; }

        /// <summary>
        /// Which data channel to send the message on.
        /// </summary>
        byte Channel { get; }

        /// <summary>
        /// Message includes a timestamp of the moment of creation.
        /// 如果为false，则由于消息延迟，接收到的时间戳可能不准确
        /// <para>If false, received timestamp might be innacurate due to message delays.</para>
        /// </summary>
        bool Timed { get; }

        /// <summary>
        /// Message requires an acknowledgment and needs to be resent until acknowledged.
        /// 消息需要确认，需要重新发送直到确认,确保信息不会丢失
        /// <para>This makes sure the message will never be lost.</para>
        /// </summary>
        bool Reliable { get; }

        /// <summary>
        /// Message must be delivered in order within the channel.
        /// 按顺序传递。任何不可靠的消息，如果不按顺序到达，将被丢弃
        /// <para>Any unreliable messages that arrive out of order are dropped.</para>
        /// <para>Any reliable messages that arrive out of order are reordered automatically.</para>
        /// </summary>
        bool Ordered { get; }

        /// <summary>
        /// Message is guaranteed not to be duplicated.
        /// 保证消息不会重复。
        /// </summary>
        bool Unique { get; }

	}
	
}
