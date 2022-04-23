using System.Collections.Generic;

namespace GLHF
{
    // TODO: Should abstract ServerInputMessage to just be a message about a specific tick.
    /// <summary>
    /// Messages inserted into the buffer are sorted by tick.
    /// </summary>
    public class MessageBuffer
    {
        public int Size => messages.Count;

        public int OldestTick => messages.Count > 0 ? messages[messages.Count - 1].Tick : -1;
        public int NewestTick => messages.Count > 0 ? messages[0].Tick : -1;

        private readonly List<ServerInputMessage> messages = new List<ServerInputMessage>();
        private readonly RollingStandardDeviation standardDeviation = new RollingStandardDeviation(100);

        private float timeLastMessageReceived;

        public void Insert(ServerInputMessage message, float time)
        {
            messages.Insert(0, message);
            standardDeviation.Insert(time - timeLastMessageReceived);

            timeLastMessageReceived = time;
        }

        public float TargetDelay()
        {
            return standardDeviation.Mean() + (standardDeviation.CalculateStandardDeviation() * 2.0f);
        }

        public float CurrentDelay(float deltaTime, float time, float playbackTime)
        {
            float timeSinceLastSnapshotReceived = time - timeLastMessageReceived;

            return NewestTick == -1 ? 0 : NewestTick * deltaTime - playbackTime + timeSinceLastSnapshotReceived;
        }

        public float CalculateError(float deltaTime, float time, float playbackTime)
        {
            float currentDelay = CurrentDelay(deltaTime, time, playbackTime);

            float error = currentDelay - TargetDelay();

            return error;
        }

        public bool TryPop(int tick, float playbackTime, float deltaTime, out ServerInputMessage message)
        {
            if (messages.Count > 0)
            {
                if (OldestTick < tick)
                {
                    throw new System.Exception($"Requesting tick {tick}, indiciating message buffer's next tick {OldestTick} has been skipped.");
                }
                else if (OldestTick == tick && playbackTime >= tick * deltaTime)
                {
                    message = messages[messages.Count - 1];
                    messages.RemoveAt(messages.Count - 1);

                    return true;
                }
            }

            message = default;
            return false;
        }
    }
}
