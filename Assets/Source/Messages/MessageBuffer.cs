using System.Collections.Generic;

namespace GLHF
{
    // TODO: Should abstract ServerInputMessage to just be a message about a specific tick.
    /// <summary>
    /// Messages inserted into the buffer are sorted by tick.
    /// </summary>
    public class MessageBuffer
    {
        public int CurrentSize => messages.Count;
        public float RttStandardDeviation => standardDeviation.CalculateStandardDeviation();
        public int NextTick => messages.Count > 0 ? messages[messages.Count - 1].Tick : -1;

        private readonly List<ServerInputMessage> messages = new List<ServerInputMessage>();
        private readonly RollingStandardDeviation standardDeviation = new RollingStandardDeviation(100);

        public void Insert(ServerInputMessage message, float rtt)
        {
            messages.Insert(0, message);
            messages.Sort(CompareMessages);            

            standardDeviation.Insert(rtt);
        }

        public bool TryPop(int tick, out ServerInputMessage message)
        {
            if (messages.Count > 0)
            {
                if (NextTick < tick)
                {
                    throw new System.Exception($"Requesting tick {tick}, indiciating message buffer's next tick {NextTick} has been skipped.");
                }
                else if (NextTick == tick)
                {
                    message = messages[messages.Count - 1];
                    messages.RemoveAt(messages.Count - 1);

                    return true;
                }
            }

            message = default;
            return false;            
        }

        private int CompareMessages(ServerInputMessage a, ServerInputMessage b)
        {
            return b.Tick - a.Tick;
        }
    }
}
