using System.Collections.Generic;

namespace GLHF
{
    public class ClientInputBuffer
    {
        public int Size => queue.Count;
        public float Error { get; private set; }

        private readonly Queue<ClientInputMessage> queue = new();

        public void Insert(ClientInputMessage message, float time, float timeUntilNextTick, int nextTick, float deltaTime)
        {
            // At what time will the next tick be simulated?
            float nextTickTime = time + timeUntilNextTick;        

            // Positive if the message is late, negative if it is early.
            int tickDelta = nextTick - message.Tick;

            // When would this message's tick be simulated?
            // I.e., to be perfectly on time, when would it arrive?
            float targetArrivalTime = nextTickTime - tickDelta * deltaTime;

            // Add buffer to account for network jitter, adjusting so that the target arrival time is slightly earlier than "perfect".
            // TODO: This buffer should resize based on the client connection's network jitter.
            float buffer = 0.02f;
            targetArrivalTime -= buffer;

            // Negative error for early messages, positive for late.
            Error = time - targetArrivalTime;

            UnityEngine.Debug.Log(Error);

            if (tickDelta <= 0)
            {
                queue.Enqueue(message);
            }
        }

        public bool TryPop(out ClientInputMessage message)
        {
            if (queue.Count > 0)
            {
                message = queue.Dequeue();

                return true;
            }
            else
            {
                message = null;
                return false;
            }
        }
    }
}
