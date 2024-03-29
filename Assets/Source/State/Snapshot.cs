namespace GLHF
{
    public unsafe class Snapshot
    {
        public int Tick
        {
            get => *tick;
            set => *tick = value;
        }

        private readonly int* tick;

        public Allocator Allocator { get; private set; }

        public Allocator.Block* FirstStateObjectBlock
        {
            get
            {
                byte* head = Allocator.Head;
                head += sizeof(Allocator.Block) + ((Allocator.Block*)head)->Size;

                return (Allocator.Block*)head;
            }
        }

        public Snapshot(Allocator allocator)
        {
            Allocator = allocator;

            tick = (int*)allocator.Allocate(sizeof(int));
        }

        public Snapshot(Snapshot from)
        {
            Allocator = new Allocator(from.Allocator);

            tick = (int*)(Allocator.Head + sizeof(Allocator.Block));
        }

        public void StateObjectAtBlock(Allocator.Block* block, out byte* ptr, out int prefabId)
        {
            ptr = (byte*)block + sizeof(Allocator.Block);
            prefabId = *(int*)(ptr + 4);
        }

        public bool NextStateObject(Allocator.Block* current, out Allocator.Block* next, out byte* ptr, out int prefabId)
        {
            if (current == null)
                current = (Allocator.Block*)Allocator.Head;

            if (Allocator.NextInUse(current, out next))
            {
                StateObjectAtBlock(next, out ptr, out prefabId);

                return true;
            }

            ptr = null;
            prefabId = 0;

            return false;
        }

        public int StateObjectCount()
        {
            int count = 0;

            Allocator.Block* current = null;

            while (NextStateObject(current, out var next, out byte* _, out int _))
            {
                count++;

                current = next;
            }

            return count;
        }

        public Allocator.Block*[] RetrieveStateObjects()
        {
            var stateObjects = new Allocator.Block*[StateObjectCount()];

            Allocator.Block* current = null;

            int i = 0;

            while (NextStateObject(current, out var next, out byte* _, out int _))
            {
                stateObjects[i] = next;
                current = next;

                i++;
            }

            return stateObjects;
        }
    }
}
