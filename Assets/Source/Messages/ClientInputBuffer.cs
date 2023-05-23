using System.Collections.Generic;

namespace GLHF
{
    public class ClientInputBuffer
    {
        public int Size => queue.Count;
        public float Error { get; private set; }

        private readonly Queue<ClientInputMessage> queue = new();
        private readonly RollingStandardDeviation standardDeviation;

        private readonly float deltaTime;

        private float lastMessageReceived;

        public ClientInputBuffer(float deltaTime)
        {
            this.deltaTime = deltaTime;

            standardDeviation = new RollingStandardDeviation((int)(1 / deltaTime));
        }

        public void Insert(ClientInputMessage message, float time, float timeUntilNextTick, int nextTick)
        {
            standardDeviation.Insert(time - lastMessageReceived);

            // At what time will the next tick be simulated?
            float nextTickTime = time + timeUntilNextTick;        

            // Positive if the message is late, negative if it is early.
            int tickDelta = nextTick - message.Tick;

            // When would this message's tick be simulated?
            // I.e., to be perfectly on time, when would it arrive?
            float targetArrivalTime = nextTickTime - tickDelta * deltaTime;

            // Add buffer to account for network jitter, adjusting so that the target arrival time is slightly earlier than "perfect".
            // TODO: This buffer should resize based on the client connection's network jitter.
            float buffer = deltaTime/* + standardDeviation.CalculateStandardDeviation() * 2*/;
            targetArrivalTime -= buffer;

            // Negative error for early messages, positive for late.
            Error = time - targetArrivalTime;

            if (tickDelta <= 0)
            {
                queue.Enqueue(message);
            }

            lastMessageReceived = time;
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
