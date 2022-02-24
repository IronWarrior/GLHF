using System;

namespace GLHF.Transport
{
    /// <summary>
    /// Allows listening or connecting to peers, and sending them
    /// blocks of byte data.
    /// </summary>
    public interface ITransport
    {
        event Action OnPeerConnected;
        
        // TODO: Should replace byte with ByteBuffer for easier use.
        event Action<int, float, byte[]> OnReceive;

        void Listen(int port);
        void Connect(string ip, int port);
        void Shutdown();
        void Update();

        // TODO: Same as above, replace with ByteBuffer.
        void SendToAll(byte[] data);
        void SetSimulatedLatency(SimulatedLatency simulatedLatency);
    }

    public struct SimulatedLatency
    {
        public int MinDelay, MaxDelay;
    }
}
