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

        // TODO: Should replace byte with ByteBuffer for easier use.
        event Action<int, float, byte[]> OnReceive;

        void Listen(int port);
        void Connect(string ip, int port);
        void Shutdown();
        void Update();

        // TODO: Same as above, replace with ByteBuffer.
        void Send(int peerId, byte[] data, DeliveryMethod deliveryMethod);
        void SendToAll(byte[] data, DeliveryMethod deliveryMethod);
        void SetSimulatedLatency(SimulatedLatency simulatedLatency);
    }
    
    // TODO: Pull this out, wrap ITransports in TransportControllers that spoof the latency?
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
