using GLHF.Transport;
using System.Collections.Generic;

namespace GLHF
{
    /// <summary>
    /// Messages sent from the server to the client.
    /// </summary>
    public class ServerInputMessage
    {
        public readonly long Checksum;
        public readonly int Tick;
        public readonly List<StateInput> Inputs;

        public ServerInputMessage(List<StateInput> inputs, int tick, long checksum)
        {
            Inputs = inputs;
            Checksum = checksum;
            Tick = tick;
        }

        public ServerInputMessage(ByteBuffer buffer)
        {
            Tick = buffer.Get<int>();
            Checksum = buffer.Get<long>();
            byte inputCount = buffer.Get<byte>();

            Inputs = new List<StateInput>(inputCount);

            for (int i = 0; i < inputCount; i++)
            {
                StateInput input = buffer.Get<StateInput>();
                Inputs.Add(input);
            }
        }

        public void Write(ByteBuffer buffer)
        {
            buffer.Put((byte)MessageType.Input);

            buffer.Put(Tick);
            buffer.Put(Checksum);
            buffer.Put((byte)Inputs.Count);

            for (int i = 0; i < Inputs.Count; i++)
            {
                buffer.Put(Inputs[i]);
            }
        }
    }
}
