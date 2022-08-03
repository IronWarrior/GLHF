using System.Collections.Generic;

namespace GLHF
{
    /// <summary>
    /// Messages inserted into the buffer are sorted by tick.
    /// </summary>
    public class MessageBuffer<T> where T : ITickMessage
    {
        public int Size => messages.Count;

        // TODO: Not a fan of signaling with negatives.
        public int OldestTick => messages.Count > 0 ? messages[messages.Count - 1].Tick : -1;
        public int NewestTick => messages.Count > 0 ? messages[0].Tick : -1;

        private readonly List<T> messages = new List<T>();
        private readonly RollingStandardDeviation standardDeviation;

        private float timeLastMessageReceived;

        // TODO: Should the buffer maintain deltatime? If the window is fixed based on it, probably.
        public MessageBuffer(float deltaTime)
        {
            standardDeviation = new RollingStandardDeviation((int)(1 / deltaTime));
        }

        public void Insert(T message, float time)
        {
            messages.Insert(0, message);
            standardDeviation.Insert(time - timeLastMessageReceived);

            timeLastMessageReceived = time;
        }

        public float TargetDelay()
        {
            return /*standardDeviation.Mean() +*/ (standardDeviation.CalculateStandardDeviation() * 3);
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

        public bool TryPop(out T message)
        {
            if (messages.Count > 0)
            {
                message = messages[messages.Count - 1];
                messages.RemoveAt(messages.Count - 1);

                return true;
            }

            message = default;
            return false;
        }

        public bool TryPop(int tick, float playbackTime, float deltaTime, out T message)
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
