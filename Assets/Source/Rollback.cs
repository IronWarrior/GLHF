namespace GLHF
{
    public class Rollback
    {
        public Snapshot Confirmed { get; private set; }
        public Snapshot Predicted { get; private set; }

        public Rollback(Snapshot snapshot)
        {
            Confirmed = snapshot;
            Predicted = new Snapshot(Confirmed);
        }

        public void CopyToPredicted()
        {
            Predicted.Allocator.CopyFrom(Confirmed.Allocator);
        }
    }
}
