using System;
using System.Net;
using ArouseBlockchain.Common;
using ArouseBlockchain.P2PNet;
using Cysharp.Threading.Tasks;
using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using static ArouseBlockchain.Common.AUtils;

namespace ArouseBlockchain.P2PNet
{
    public abstract class INetClient
    {
        protected Peer Peer;
        public INetClient(Peer peer) { Peer = peer; }
        public INetClient() { }

    }

}

