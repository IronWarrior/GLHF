using LiteNetLib;
using System;

namespace GLHF.Transport
{
    public class TransportLiteNetLib : ITransport
    {
        public event Action OnPeerConnected;
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

        public void Send(int peer, byte[] data)
        {
            netManager.ConnectedPeerList[peer].Send(data, DeliveryMethod.ReliableOrdered);
        }

        public void SendToAll(byte[] data)
        {
            netManager.SendToAll(data, DeliveryMethod.ReliableOrdered);
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
            OnPeerConnected?.Invoke();
        }

        private void NetworkErrorEvent(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        {
        }

        private void NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        { 
            byte[] bytes = reader.GetRemainingBytes();

            OnReceive?.Invoke(peer.Id, peer.Ping, bytes);
        }
    }
}