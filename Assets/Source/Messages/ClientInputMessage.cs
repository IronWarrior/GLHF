using GGEZ.Transport;

namespace GGEZ
{
    /// <summary>
    /// Messages sent from the client to the server.
    /// </summary>
    public class ClientInputMessage
    {
        public readonly StateInput Input;

        public ClientInputMessage(StateInput input)
        {
            Input = input;
        }

        public ClientInputMessage(ByteBuffer buffer)
        {
            Input = buffer.Get<StateInput>();
        }

        public void Write(ByteBuffer buffer)
        {
            buffer.Put((byte)MessageType.Input);
            buffer.Put(Input);
        }
    }
}
