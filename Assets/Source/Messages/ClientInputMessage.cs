using GLHF.Transport;

namespace GLHF
{
    /// <summary>
    /// Messages sent from the client to the server.
    /// </summary>
    public class ClientInputMessage
    {
        public readonly StateInput Input;
        public readonly int Tick;

        public ClientInputMessage(StateInput input, int tick)
        {
            Input = input;
            Tick = tick;
        }

        public ClientInputMessage(ByteBuffer buffer)
        {
            Tick = buffer.Get<int>();
            Input = buffer.Get<StateInput>();
        }

        public void Write(ByteBuffer buffer)
        {
            buffer.Put((byte)MessageType.Input);
            buffer.Put(Tick);
            buffer.Put(Input);
        }
    }
}
