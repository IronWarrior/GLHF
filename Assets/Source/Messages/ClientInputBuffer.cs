using System.Collections.Generic;

namespace GLHF
{
    // TODO: When updated for prediction, would report receive error, etc.
    public class ClientInputBuffer
    {
        public int Size => queue.Count;
        public float Error { get; private set; }

        //private readonly OrderedMessageBuffer<ClientInputMessage> unconsumed = new OrderedMessageBuffer<ClientInputMessage>();
        //private readonly MessageBuffer<ClientInputMessage> pending = new MessageBuffer<ClientInputMessage>(1);

        private readonly Queue<ClientInputMessage> queue = new Queue<ClientInputMessage>();

        private int nextTickToBeConsumed;

        public ClientInputBuffer()
        {

        }

        public ClientInputBuffer(int firstTickToBeConsumed)
        {
            nextTickToBeConsumed = firstTickToBeConsumed;
        }

        public void Insert(ClientInputMessage message, int nextTick, float timeUntilNextTick, float deltaTime)
        {
            // Negative if the message has arrived early, positive if it is late.
            int tickDelta = nextTick - message.Tick;

            // Also should add in some standard dev as a buffer, based on the regularity of the arrivals.
            Error = tickDelta * deltaTime;

            if (tickDelta <= 0)
            {
                queue.Enqueue(message);
            }

            // unconsumed.Insert(message);
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

            //while (unconsumed.TryDequeue(nextTickToBeConsumed, out ClientInputMessage unconsumedMessage))
            //{
            //    // Not really using the time...yet.
            //    pending.Insert(unconsumedMessage, 0);

            //    nextTickToBeConsumed++;
            //}

            //if (pending.TryPop(out message))
            //{
            //    return true;
            //}
            //else
            //{
            //    return false;
            //}
        }
    }
}
