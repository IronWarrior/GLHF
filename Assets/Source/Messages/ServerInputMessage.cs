using GLHF.Transport;
using System.Collections.Generic;

namespace GLHF
{
    /// <summary>
    /// Messages sent from the server to the client.
    /// </summary>
    public class ServerInputMessage : ITickMessage
    {
        public readonly long Checksum;

        public int Tick => tick;
        private readonly int tick;

        public readonly List<StateInput> Inputs;

        public readonly int NewPlayersJoining;
        public readonly float RequestedInputTimingDelta;

        public ServerInputMessage(List<StateInput> inputs, int tick, long checksum, int newPlayersJoining, float requestedInputTimingDelta)
        {
            Inputs = inputs;
            Checksum = checksum;
            this.tick = tick;

            NewPlayersJoining = newPlayersJoining;
            RequestedInputTimingDelta = requestedInputTimingDelta;
        }

        public ServerInputMessage(ByteBuffer buffer)
        {
            tick = buffer.Get<int>();
            Checksum = buffer.Get<long>();
            byte inputCount = buffer.Get<byte>();

            Inputs = new List<StateInput>(inputCount);

            for (int i = 0; i < inputCount; i++)
            {
                StateInput input = buffer.Get<StateInput>();
                Inputs.Add(input);
            }

            NewPlayersJoining = buffer.Get<int>();
            RequestedInputTimingDelta = buffer.Get<float>();
        }

        public void Write(ByteBuffer buffer)
        {
            buffer.Put((byte)MessageType.Input);

            buffer.Put(tick);
            buffer.Put(Checksum);
            buffer.Put((byte)Inputs.Count);

            for (int i = 0; i < Inputs.Count; i++)
            {
                buffer.Put(Inputs[i]);
            }

            buffer.Put(NewPlayersJoining);
            buffer.Put(RequestedInputTimingDelta);
        }
    }
}
