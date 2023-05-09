using LiteNetLib;
using System;
using System.Collections.Generic;

namespace GLHF.Transport
{
    public class TransportLiteNetLib : ITransport
    {
        public event Action<int> OnPeerConnected;
        public event Action<int> OnPeerDisconnected;
        public event Action<int, float, byte[]> OnReceive;

        private readonly NetManager netManager;
        private readonly EventBasedNetListener listener;

        private readonly Dictionary<int, NetPeer> peers = new Dictionary<int, NetPeer>();

        public TransportLiteNetLib()
        {
            listener = new EventBasedNetListener();
            listener.NetworkReceiveEvent += NetworkReceiveEvent;
            listener.PeerConnectedEvent += PeerConnectedEvent;
            listener.PeerDisconnectedEvent += PeerDisconnectedEvent;

            netManager = new NetManager(listener);
        }

        public void Listen(int port)
        {
            listener.ConnectionRequestEvent += ConnectionRequestEvent;

            netManager.Start(port);
        }

        public void Connect(string ip, int port)
        {
            netManager.Start();
            netManager.Connect(ip, port, "Test");
        }

        public void Poll()
        {
            netManager.PollEvents();
        }

        public void Shutdown()
        {
            netManager.Stop();
        }

        public void Send(int peerId, byte[] data, DeliveryMethod deliveryMethod)
        {
            peers[peerId].Send(data, GetDeliveryMethod(deliveryMethod));
        }

        public void SendToAll(byte[] data, DeliveryMethod deliveryMethod)
        {
            foreach (var peer in peers.Values)
                peer.Send(data, GetDeliveryMethod(deliveryMethod));
        }

        private void ConnectionRequestEvent(ConnectionRequest request)
        {
            request.Accept();
        }

        private void PeerConnectedEvent(NetPeer peer)
        {
            peers.Add(peer.Id, peer);

            OnPeerConnected?.Invoke(peer.Id);
        }

        private void PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            peers.Remove(peer.Id);

            OnPeerDisconnected?.Invoke(peer.Id);
        }

        private void NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, LiteNetLib.DeliveryMethod deliveryMethod)
        { 
            byte[] bytes = reader.GetRemainingBytes();

            OnReceive?.Invoke(peer.Id, peer.Ping, bytes);
        }

        private LiteNetLib.DeliveryMethod GetDeliveryMethod(DeliveryMethod method)
        {
            switch (method)
            {
                case DeliveryMethod.Reliable:
                    return LiteNetLib.DeliveryMethod.ReliableUnordered;
                case DeliveryMethod.ReliableOrdered:
                    return LiteNetLib.DeliveryMethod.ReliableOrdered;
                default:
                    return LiteNetLib.DeliveryMethod.ReliableUnordered;
            }
        }
    }
}