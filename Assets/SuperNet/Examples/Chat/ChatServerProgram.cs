using SuperNet.Netcode.Transport;
using SuperNet.Netcode.Util;
using System;
using System.Net;
using System.Threading;

namespace SuperNet.Examples.Chat {

	public class ChatServerProgram {

		// This is a chat server program that can be compiled without Unity
		// Once running, you can connect to it with the chat client
		// Make sure the UDP port 44015 is open on the server
		// You can change the port in the Main() method

		public static void Main(string[] args) {
			// Create the program and wait forever
			new ChatServerProgram(44015);
			Thread.Sleep(Timeout.Infinite);
		}

		private readonly Host Host;

		public ChatServerProgram(int port) {

			// Register host events
			HostEvents events = new HostEvents();
			events.OnException += OnHostException;
			events.OnReceiveRequest += OnHostReceiveRequest;

			// Create host config
			HostConfig config = new HostConfig();
			config.Port = port;

			// Create host
			Host = new Host(config, events);
			Console.WriteLine("Chat server started on: " + Host.BindAddress);

		}

		private void OnHostException(IPEndPoint remote, Exception exception) {
			Console.WriteLine("Host exception: " + exception.Message);
		}

		private void OnHostReceiveRequest(ConnectionRequest request, Reader message) {

			// Register peer events
			PeerEvents events = new PeerEvents();
			events.OnConnect += OnPeerConnect;
			events.OnDisconnect += OnPeerDisconnect;
			events.OnException += OnPeerException;
			events.OnReceive += OnPeerReceive;

			// Accept connection
			request.Accept(new PeerConfig(), events);

		}

		private void OnPeerConnect(Peer peer) {

			// Send join message to all clients
			ServerMessageJoin join = new ServerMessageJoin();
			join.Name = peer.Remote.ToString();
			Host.SendAll(join, peer);

			// Notify console
			Console.WriteLine("[" + peer.Remote + "] Connected.");

		}

		private void OnPeerDisconnect(Peer peer, Reader message, DisconnectReason reason, Exception exception) {

			// Send leave message to all clients
			ServerMessageLeave leave = new ServerMessageLeave();
			leave.Name = peer.Remote.ToString();
			Host.SendAll(leave, peer);

			// Notify console
			Console.WriteLine("[" + peer.Remote + "] Disconnected: " + reason);

		}

		private void OnPeerException(Peer peer, Exception exception) {
			// Exceptions don't usually indicate any errors, we can ignore them
			Console.WriteLine("[" + peer.Remote + "] Exception: " + exception.Message);
		}

		private void OnPeerReceive(Peer peer, Reader message, MessageReceived info) {

			// Convert channel to message type
			ClientMessageType type = (ClientMessageType)info.Channel;

			// Process message based on type
			switch (type) {
				case ClientMessageType.Chat:
					OnPeerReceiveChat(peer, message, info);
					break;
				default:
					Console.WriteLine("[" + peer.Remote + "] Sent an unknown message type " + info.Channel);
					break;
			}

		}

		private void OnPeerReceiveChat(Peer peer, Reader reader, MessageReceived info) {

			// Read the message
			ClientMessageChat message = new ClientMessageChat();
			message.Read(reader);

			// Send message to all clients including the one who sent it
			ServerMessageChat chat = new ServerMessageChat();
			chat.Name = peer.Remote.ToString();
			chat.Message = message.Message;
			Host.SendAll(chat);

			// Notify console
			Console.WriteLine("[" + peer.Remote + "] Says: " + message.Message);

		}

	}

}
