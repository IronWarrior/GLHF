using GLHF.Transport;

namespace GLHF
{
    /// <summary>
    /// Messages sent from the client to the server.
    /// </summary>
    public class ClientInputMessage : ITickMessage
    {
        public readonly StateInput Input;

        public int Tick => tick;
        private readonly int tick;

        public ClientInputMessage(StateInput input, int tick)
        {
            Input = input;
            this.tick = tick;
        }

        public ClientInputMessage(ByteBuffer buffer)
        {
            tick = buffer.Get<int>();
            Input = buffer.Get<StateInput>();
        }

        public void Write(ByteBuffer buffer)
        {
            buffer.Put((byte)MessageType.Input);
            buffer.Put(tick);
            buffer.Put(Input);
        }
    }
}
