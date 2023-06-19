using ArouseBlockchain.P2PNet;
using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace ArouseBlockchain.P2PNet
{
    #region 网络传输中的数据结构
    public enum RelayMessageType : byte
    {

        // Messages between clients and relay
        // 1. A客户端访问中继获取 Peers [PeerListRequest]
        // 2. 并且 A-Host 链接到 Host-B, 返回B的Peer
        // 3. 并且通知中继，A要去链接B。
        // 4. 中继收到之后，找到 Host-B，并发送A的消息
        // 5. Host-B 主动链接到 客户端 A
        // 6. 完成通讯
        Connect = 1,

        // 1. A客户端请求Peer列表
        PeerListRequest = 2,
        // 2. 服务器返回列表
        PeerListResponse = 3,

        //1. A客户端请求添加或更新房间(房间有人进入有人离开)
        ServerUpdate = 4,
        //1. A客户端请求移除房间
        ServerRemove = 5,

    }

    public struct RelayListRequest : SuperNet.Netcode.Transport.IMessage
    {
        // This message is sent by the client to request a list of peers on the relay

        HostTimestamp SuperNet.Netcode.Transport.IMessage.Timestamp => Host.Now;
        byte SuperNet.Netcode.Transport.IMessage.Channel => (byte)RelayMessageType.PeerListRequest;
        bool SuperNet.Netcode.Transport.IMessage.Timed => false;
        bool SuperNet.Netcode.Transport.IMessage.Reliable => true;
        bool SuperNet.Netcode.Transport.IMessage.Ordered => false;
        bool SuperNet.Netcode.Transport.IMessage.Unique => true;

        public void Read(Reader reader) { }
        public void Write(Writer writer) { }

    }

    public struct RelayPeerNetInfoEntry
    {
        // This structure contains the server information reported by the relay
        public string AddressLocal;
        public string AddressRemote;
        public string AddressPublic;

        public string Name;
        public int Players;

        public void Read(Reader reader)
        {
            AddressLocal = reader.ReadString();
            AddressRemote = reader.ReadString();
            AddressPublic = reader.ReadString();
            Name = reader.ReadString();
            Players = reader.ReadInt32();
        }

        public void Write(Writer writer)
        {
            writer.Write(AddressLocal);
            writer.Write(AddressRemote);
            writer.Write(AddressPublic);
            writer.Write(Name);
            writer.Write(Players);
        }

    }

    public struct RelayListResponse : IMessage
    {

        // This message is sent by the relay to the client after a list of peers is requested
        // It contains an entry for each server on the relay

        HostTimestamp SuperNet.Netcode.Transport.IMessage.Timestamp => Host.Now;
        byte SuperNet.Netcode.Transport.IMessage.Channel => (byte)RelayMessageType.PeerListResponse;
        bool SuperNet.Netcode.Transport.IMessage.Timed => false;
        bool SuperNet.Netcode.Transport.IMessage.Reliable => true;
        bool SuperNet.Netcode.Transport.IMessage.Ordered => false;
        bool SuperNet.Netcode.Transport.IMessage.Unique => true;

        public List<RelayPeerNetInfoEntry> Servers;

        public void Read(Reader reader)
        {
            Servers = new List<RelayPeerNetInfoEntry>();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                RelayPeerNetInfoEntry entry = new RelayPeerNetInfoEntry();
                entry.Read(reader);
                Servers.Add(entry);
            }
        }

        public void Write(Writer writer)
        {
            writer.Write(Servers.Count);
            foreach (RelayPeerNetInfoEntry server in Servers)
            {
                server.Write(writer);
            }
        }

    }

    public struct RelayServerUpdate : SuperNet.Netcode.Transport.IMessage
    {

        // This message is sent by the client that just started hosting a server
        // When the relay receives this message it adds the server to the list
        // This message is also sent when the server information changes

        HostTimestamp SuperNet.Netcode.Transport.IMessage.Timestamp => Host.Now;
        byte SuperNet.Netcode.Transport.IMessage.Channel => (byte)RelayMessageType.ServerUpdate;
        bool SuperNet.Netcode.Transport.IMessage.Timed => false;
        bool SuperNet.Netcode.Transport.IMessage.Reliable => true;
        bool SuperNet.Netcode.Transport.IMessage.Ordered => false;
        bool SuperNet.Netcode.Transport.IMessage.Unique => true;

        public RelayPeerNetInfoEntry Entry;

        public void Read(Reader reader)
        {
            Entry = new RelayPeerNetInfoEntry();
            Entry.Read(reader);
        }

        public void Write(Writer writer)
        {
            Entry.Write(writer);
        }

    }

    public struct RelayServerRemove : SuperNet.Netcode.Transport.IMessage
    {

        // This message is sent by the client to request its server to be removed
        // The relay doesn't respond to this message, it just removes the previously added server

        HostTimestamp SuperNet.Netcode.Transport.IMessage.Timestamp => Host.Now;
        byte SuperNet.Netcode.Transport.IMessage.Channel => (byte)RelayMessageType.ServerRemove;
        bool SuperNet.Netcode.Transport.IMessage.Timed => false;
        bool SuperNet.Netcode.Transport.IMessage.Reliable => true;
        bool SuperNet.Netcode.Transport.IMessage.Ordered => false;
        bool SuperNet.Netcode.Transport.IMessage.Unique => true;

        public void Read(Reader reader) { }
        public void Write(Writer writer) { }

    }

    public struct RelayConnect : SuperNet.Netcode.Transport.IMessage
    {

        // This message is sent by the client to the relay when connecting to a server
        // The relay then sends this message to the server the client is connecting to
        // The server then starts connecting to the client while the client is connecting to the server
        // Because both the server and the client are connecting to each other, a p2p connection is established

        HostTimestamp SuperNet.Netcode.Transport.IMessage.Timestamp => Host.Now;
        byte SuperNet.Netcode.Transport.IMessage.Channel => (byte)RelayMessageType.Connect;
        bool SuperNet.Netcode.Transport.IMessage.Timed => false;
        bool SuperNet.Netcode.Transport.IMessage.Reliable => true;
        bool SuperNet.Netcode.Transport.IMessage.Ordered => false;
        bool SuperNet.Netcode.Transport.IMessage.Unique => true;

        public string Address;

        public void Read(Reader reader)
        {
            Address = reader.ReadString();
        }

        public void Write(Writer writer)
        {
            writer.Write(Address);
        }

    }
    #endregion


}
