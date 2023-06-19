using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using System;
using System.Threading;

namespace SuperNet.Unity.Core {

	internal class NetworkMessageCopy : IMessage, IMessageListener {

		HostTimestamp IMessage.Timestamp => Info.Timestamp;
		byte IMessage.Channel => Info.Channel;
		bool IMessage.Timed => Info.Timed;
		bool IMessage.Reliable => Info.Reliable;
		bool IMessage.Ordered => Info.Ordered;
		bool IMessage.Unique => Info.Unique;
		
		public readonly Allocator Allocator;
		public readonly MessageReceived Info;
		public readonly int Length;
		public byte[] Buffer;
		public int Count;

		public NetworkMessageCopy(Allocator allocator, Reader reader, MessageReceived info) {
			Allocator = allocator;
			Info = info;
			Length = reader.Last - reader.First;
			Buffer = allocator.CreateMessage(Length);
			Array.Copy(reader.Buffer, reader.First, Buffer, 0, Length);
			Count = 0;
		}
		
		public void Send(Peer peer) {
			Interlocked.Increment(ref Count);
			peer.Send(this, this);
		}

		void IWritable.Write(Writer writer) {
			byte[] buffer = Interlocked.CompareExchange(ref Buffer, null, null);
			if (buffer != null) {
				writer.WriteBytes(buffer, 0, Length);
			}
		}

		void IMessageListener.OnMessageSend(Peer peer, MessageSent message) {
			if (Info.Reliable) return;
			int count = Interlocked.Decrement(ref Count);
			if (count == 1) Allocator.ReturnMessage(ref Buffer);
		}

		void IMessageListener.OnMessageAcknowledge(Peer peer, MessageSent message) {
			int count = Interlocked.Decrement(ref Count);
			if (count == 1) Allocator.ReturnMessage(ref Buffer);
		}

	}

}
