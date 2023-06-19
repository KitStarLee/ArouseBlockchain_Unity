using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using System.Collections.Generic;

namespace SuperNet.Examples.Arena {

	public enum RelayMessageType : byte {

		// Messages between clients and relay
		Connect = 251,
		ListRequest = 250,
		ListResponse = 249,
		ServerUpdate = 248,
		ServerRemove = 247,

	}

	public struct RelayListRequest : IMessage {

		// This message is sent by the client to request a list of servers on the relay
		// The relay responds to this message with a RelayListResponse message

		HostTimestamp IMessage.Timestamp => Host.Now;
		byte IMessage.Channel => (byte)RelayMessageType.ListRequest;
		bool IMessage.Timed => false;
		bool IMessage.Reliable => true;
		bool IMessage.Ordered => false;
		bool IMessage.Unique => true;
		
		public void Read(Reader reader) { }
		public void Write(Writer writer) { }

	}

	public struct RelayListResponseEntry {

		// This structure contains the server information reported by the relay

		public string AddressLocal;
		public string AddressRemote;
		public string AddressPublic;
		public string Name;
		public int Players;

		public void Read(Reader reader) {
			AddressLocal = reader.ReadString();
			AddressRemote = reader.ReadString();
			AddressPublic = reader.ReadString();
			Name = reader.ReadString();
			Players = reader.ReadInt32();
		}

		public void Write(Writer writer) {
			writer.Write(AddressLocal);
			writer.Write(AddressRemote);
			writer.Write(AddressPublic);
			writer.Write(Name);
			writer.Write(Players);
		}

	}

	public struct RelayListResponse : IMessage {

		// This message is sent by the relay to the client after a list of servers is requested
		// It contains an entry for each server on the relay

		HostTimestamp IMessage.Timestamp => Host.Now;
		byte IMessage.Channel => (byte)RelayMessageType.ListResponse;
		bool IMessage.Timed => false;
		bool IMessage.Reliable => true;
		bool IMessage.Ordered => false;
		bool IMessage.Unique => true;
		
		public List<RelayListResponseEntry> Servers;

		public void Read(Reader reader) {
			Servers = new List<RelayListResponseEntry>();
			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++) {
				RelayListResponseEntry entry = new RelayListResponseEntry();
				entry.Read(reader);
				Servers.Add(entry);
			}
		}

		public void Write(Writer writer) {
			writer.Write(Servers.Count);
			foreach (RelayListResponseEntry server in Servers) {
				server.Write(writer);
			}
		}

	}

	public struct RelayServerUpdate : IMessage {

		// This message is sent by the client that just started hosting a server
		// When the relay receives this message it adds the server to the list
		// This message is also sent when the server information changes

		HostTimestamp IMessage.Timestamp => Host.Now;
		byte IMessage.Channel => (byte)RelayMessageType.ServerUpdate;
		bool IMessage.Timed => false;
		bool IMessage.Reliable => true;
		bool IMessage.Ordered => false;
		bool IMessage.Unique => true;
		
		public RelayListResponseEntry Entry;

		public void Read(Reader reader) {
			Entry = new RelayListResponseEntry();
			Entry.Read(reader);
		}

		public void Write(Writer writer) {
			Entry.Write(writer);
		}

	}

	public struct RelayServerRemove : IMessage {

		// This message is sent by the client to request its server to be removed
		// The relay doesn't respond to this message, it just removes the previously added server

		HostTimestamp IMessage.Timestamp => Host.Now;
		byte IMessage.Channel => (byte)RelayMessageType.ServerRemove;
		bool IMessage.Timed => false;
		bool IMessage.Reliable => true;
		bool IMessage.Ordered => false;
		bool IMessage.Unique => true;

		public void Read(Reader reader) { }
		public void Write(Writer writer) { }

	}

	public struct RelayConnect : IMessage {

		// This message is sent by the client to the relay when connecting to a server
		// The relay then sends this message to the server the client is connecting to
		// The server then starts connecting to the client while the client is connecting to the server
		// Because both the server and the client are connecting to each other, a p2p connection is established

		HostTimestamp IMessage.Timestamp => Host.Now;
		byte IMessage.Channel => (byte)RelayMessageType.Connect;
		bool IMessage.Timed => false;
		bool IMessage.Reliable => true;
		bool IMessage.Ordered => false;
		bool IMessage.Unique => true;
		
		public string Address;

		public void Read(Reader reader) {
			Address = reader.ReadString();
		}

		public void Write(Writer writer) {
			writer.Write(Address);
		}

	}

}
