using System.Collections.Generic;

namespace GLHF
{
    public class Rollback
    {
        public Snapshot Snapshot { get; private set; }
        public Snapshot Confirmed { get; private set; }

        public int ForwardTick { get; set; } = -1;

        private readonly Dictionary<int, StateInput> predictedInputs = new();

        public Rollback(Snapshot snapshot)
        {
            Snapshot = snapshot;
            Confirmed = new Snapshot(Snapshot);
        }

        public void PushSnapshotToConfirmed()
        {
            Confirmed.Allocator.CopyFrom(Snapshot.Allocator);
        }

        public void PopConfirmedToSnapshot()
        {
            Snapshot.Allocator.CopyFrom(Confirmed.Allocator);
        }

        public void InsertPredictedInput(int tick, StateInput input)
        {
            predictedInputs[tick] = input;
        }

        public StateInput GetPredictedInput(int tick)
        {
            if (predictedInputs.TryGetValue(tick, out StateInput input))
                return input;
            else
                return default;
        }

        public void ConsumePredictedInput(int tick)
        {
            if (predictedInputs.ContainsKey(tick))
                predictedInputs.Remove(tick);
        }
    }
}
