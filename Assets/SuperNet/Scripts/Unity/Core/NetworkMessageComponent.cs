using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;

namespace SuperNet.Unity.Core {

	internal struct NetworkMessageComponent : IMessage {

		HostTimestamp IMessage.Timestamp => Message.Timestamp;
		byte IMessage.Channel => Message.Channel;
		bool IMessage.Timed => Message.Timed;
		bool IMessage.Reliable => Message.Reliable;
		bool IMessage.Ordered => Message.Ordered;
		bool IMessage.Unique => Message.Unique;
		
		public byte Hops;
		public byte TTL;
		public ushort Sequence;
		public NetworkIdentity NetworkID;
		public IMessage Message;

		public void Write(Writer writer) {
			writer.Write(Hops);
			writer.Write(TTL);
			writer.Write(Sequence);
			writer.Write(NetworkID.Value);
			Message.Write(writer);
		}

		public void ReadHeader(Reader reader) {
			Hops = reader.ReadByte();
			TTL = reader.ReadByte();
			Sequence = reader.ReadUInt16();
			NetworkID = reader.ReadNetworkID();
		}

	}

}
