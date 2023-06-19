namespace SuperNet.Netcode.Transport {
	
	/// <summary>
	/// Implements a sent message listener.
	/// </summary>
	public interface IMessageListener {

		/// <summary>Called after the message gets sent to the socket.</summary>
		/// <param name="peer">Peer that sent the message.</param>
		/// <param name="message">Message that was sent.</param>
		void OnMessageSend(Peer peer, MessageSent message);

		/// <summary>Called when a reliable message gets acknowledged.</summary>
		/// <param name="peer">Peer that received the acknowledgment.</param>
		/// <param name="message">Message that was acknowledged.</param>
		void OnMessageAcknowledge(Peer peer, MessageSent message);

	}

	/// <summary>
	/// Event based implementation of a message listener.
	/// </summary>
	public class MessageEvents : IMessageListener {

		public delegate void OnSendHandler(Peer peer, MessageSent message);
		public delegate void OnAcknowledgeHandler(Peer peer, MessageSent message);

		/// <summary>Called after the message gets sent to the socket.</summary>
		public event OnSendHandler OnSend;

		/// <summary>Called when a reliable message gets acknowledged.</summary>
		public event OnAcknowledgeHandler OnAcknowledge;

		void IMessageListener.OnMessageSend(Peer peer, MessageSent message) {
			OnSend?.Invoke(peer, message);
		}

		void IMessageListener.OnMessageAcknowledge(Peer peer, MessageSent message) {
			OnAcknowledge?.Invoke(peer, message);
		}

	}

}
