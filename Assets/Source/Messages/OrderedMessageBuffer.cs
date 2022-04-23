using System.Collections.Generic;

namespace GLHF
{
    public class OrderedMessageBuffer
    {
        private readonly List<ServerInputMessage> messages = new List<ServerInputMessage>();

        public void Insert(ServerInputMessage message)
        {
            messages.Add(message);
            messages.Sort(CompareMessages);
        }

        public bool TryDequeue(int targetTick, out ServerInputMessage message)
        {
            if (messages.Count > 0)
            {
                ServerInputMessage oldest = messages[messages.Count - 1];

                if (oldest.Tick == targetTick)
                {
                    messages.RemoveAt(messages.Count - 1);
                    message = oldest;

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
