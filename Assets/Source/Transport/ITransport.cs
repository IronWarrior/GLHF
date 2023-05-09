using System;

namespace GLHF.Transport
{
    /// <summary>
    /// Allows listening or connecting to peers, and sending them
    /// blocks of byte data.
    /// </summary>
    public interface ITransport
    {
        event Action<int> OnPeerConnected;
        event Action<int> OnPeerDisconnected;
        event Action<int, float, byte[]> OnReceive;

        void Listen(int port);
        void Connect(string ip, int port);
        void Shutdown();
        void Poll();

        void Send(int peerId, byte[] data, DeliveryMethod deliveryMethod);
        void SendToAll(byte[] data, DeliveryMethod deliveryMethod);
    }
    
    public struct SimulatedLatency
    {
        public int MinDelay, MaxDelay;
    }

    public enum DeliveryMethod
    {
        Reliable = 0,
        ReliableOrdered = 1
    }
}
