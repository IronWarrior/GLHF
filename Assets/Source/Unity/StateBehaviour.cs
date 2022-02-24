namespace GLHF
{
    public unsafe abstract class StateBehaviour : TickBehaviour
    {
        public byte* Ptr { get; set; }
        public abstract int Size { get; }

        public void IncrementPointer(int offset)
        {
            Ptr += offset;
        }
    }
}