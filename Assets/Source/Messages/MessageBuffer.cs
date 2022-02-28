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

        private readonly List<ServerInputMessage> messages = new List<ServerInputMessage>();

        public void Insert(ServerInputMessage message)
        {
            messages.Insert(0, message);
            messages.Sort(CompareMessages);
        }

        public bool TryPop(int tick, out ServerInputMessage message)
        {
            if (messages.Count > 0 && messages[messages.Count - 1].Tick == tick)
            {
                message = messages[messages.Count - 1];
                messages.RemoveAt(messages.Count - 1);

                return true;
            }
            else
            {
                message = default;

                return false;
            }
        }

        private int CompareMessages(ServerInputMessage a, ServerInputMessage b)
        {
            return b.Tick - a.Tick;
        }
    }
}