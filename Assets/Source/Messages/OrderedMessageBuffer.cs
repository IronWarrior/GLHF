using System.Collections.Generic;

namespace GLHF
{
    public class OrderedMessageBuffer<T> where T : ITickMessage
    {
        private readonly List<T> messages = new List<T>();

        public void Insert(T message)
        {
            messages.Add(message);
            messages.Sort(CompareMessages);
        }

        public bool TryDequeue(int targetTick, out T message)
        {
            if (messages.Count > 0)
            {
                T oldest = messages[messages.Count - 1];

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

        private int CompareMessages(T a, T b)
        {
            return b.Tick - a.Tick;
        }
    }
}
