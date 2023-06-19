using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;

namespace SuperNet.Examples.Chat {

	public enum ServerMessageType : byte {

		Join = 1,
		Leave = 2,
		Chat = 3,

	}

	public enum ClientMessageType : byte {

		Chat = 1,

	}

	// Sent by the server to clients when a new client joins
	public struct ServerMessageJoin : IMessage {

		HostTimestamp IMessage.Timestamp => Host.Now;
		byte IMessage.Channel => (byte)ServerMessageType.Join;
		bool IMessage.Timed => false;
		bool IMessage.Reliable => true;
		bool IMessage.Ordered => true;
		bool IMessage.Unique => true;
		
		public string Name;

		public void Read(Reader reader) {
			Name = reader.ReadString();
		}

		public void Write(Writer writer) {
			writer.Write(Name);
		}

	}

	// Sent by the server to clients when a client leaves
	public struct ServerMessageLeave : IMessage {

		HostTimestamp IMessage.Timestamp => Host.Now;
		byte IMessage.Channel => (byte)ServerMessageType.Leave;
		bool IMessage.Timed => false;
		bool IMessage.Reliable => true;
		bool IMessage.Ordered => true;
		bool IMessage.Unique => true;
		
		public string Name;

		public void Read(Reader reader) {
			Name = reader.ReadString();
		}

		public void Write(Writer writer) {
			writer.Write(Name);
		}

	}

	// Sent by the server to clients when a client sends a chat message
	public struct ServerMessageChat : IMessage {

		HostTimestamp IMessage.Timestamp => Host.Now;
		byte IMessage.Channel => (byte)ServerMessageType.Chat;
		bool IMessage.Timed => false;
		bool IMessage.Reliable => true;
		bool IMessage.Ordered => false;
		bool IMessage.Unique => true;
		
		public string Name;
		public string Message;

		public void Write(Writer writer) {
			writer.Write(Name);
			writer.Write(Message);
		}

		public void Read(Reader reader) {
			Name = reader.ReadString();
			Message = reader.ReadString();
		}

	}

	// Sent by the client to server when a chat message is sent
	public struct ClientMessageChat : IMessage {

		HostTimestamp IMessage.Timestamp => Host.Now;
		byte IMessage.Channel => (byte)ClientMessageType.Chat;
		bool IMessage.Timed => false;
		bool IMessage.Reliable => true;
		bool IMessage.Ordered => true;
		bool IMessage.Unique => true;
		
		public string Message;

		public void Read(Reader reader) {
			Message = reader.ReadString();
		}

		public void Write(Writer writer) {
			writer.Write(Message);
		}

	}

}
