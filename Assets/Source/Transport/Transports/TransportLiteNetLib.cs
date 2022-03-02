using LiteNetLib;
using System;

namespace GLHF.Transport
{
    public class TransportLiteNetLib : ITransport
    {
        public event Action<int> OnPeerConnected;
        public event Action<int, float, byte[]> OnReceive;

        private readonly NetManager netManager;
        private readonly EventBasedNetListener listener;

        public TransportLiteNetLib()
        {
            listener = new EventBasedNetListener();
            listener.NetworkReceiveEvent += NetworkReceiveEvent;
            listener.PeerConnectedEvent += PeerConnectedEvent;
            listener.NetworkErrorEvent += NetworkErrorEvent;

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

        public void Update()
        {
            netManager.PollEvents();
        }

        public void Shutdown()
        {
            netManager.Stop();
        }

        public void Send(int peerId, byte[] data, DeliveryMethod deliveryMethod)
        {
            netManager.ConnectedPeerList[peerId].Send(data, GetDeliveryMethod(deliveryMethod));
        }

        public void SendToAll(byte[] data, DeliveryMethod deliveryMethod)
        {
            netManager.SendToAll(data, GetDeliveryMethod(deliveryMethod));
        }

        public void SetSimulatedLatency(SimulatedLatency simulatedLatency)
        {
            throw new NotImplementedException();
        }

        private void ConnectionRequestEvent(ConnectionRequest request)
        {
            request.Accept();
        }

        private void PeerConnectedEvent(NetPeer peer)
        {
            OnPeerConnected?.Invoke(peer.Id);
        }

        private void NetworkErrorEvent(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        {
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