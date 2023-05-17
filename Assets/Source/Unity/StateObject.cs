using System.Collections.Generic;

namespace GLHF
{
    public unsafe class StateObject : TickBehaviour
    {
        public int Size { get; private set; }
        public byte* Ptr { get; private set; }

        public int Id
        {
            get => *(int*)Ptr;
            set => *(int*)Ptr = value;
        }

        public int PrefabId
        {
            get => *(int*)(Ptr + 4);
            set => *(int*)(Ptr + 4) = value;
        }

        // TODO: Uncomment when tested.
        // [HideInInspector]
        public int BakedPrefabId = -1;

        public bool IsSceneObject => BakedPrefabId < 0;
        public bool Spawned => Ptr != null;

        private List<TickBehaviour> tickBehaviours;
        private List<StateBehaviour> stateBehaviours;

        private void Awake()
        {
            tickBehaviours = new List<TickBehaviour>(GetComponentsInChildren<TickBehaviour>());
            tickBehaviours.Remove(this);

            stateBehaviours = new List<StateBehaviour>(GetComponentsInChildren<StateBehaviour>());

            Size = sizeof(int) + sizeof(int);

            Object = this;

            foreach (var sb in stateBehaviours)
            {
                Size += sb.Size;
            }

            foreach (var tb in tickBehaviours)
            {
                tb.Object = this;
            }
        }

        internal void SetSimulation(Simulation simulation)
        {
            Simulation = simulation;

            foreach (var tb in tickBehaviours)
            {
                tb.Simulation = Simulation;
            }

            foreach (var tb in tickBehaviours)
            {
                tb.Initialized();
            }
        }

        internal void SetPointer(byte* ptr)
        {
            Ptr = ptr;

            PrefabId = BakedPrefabId;

            int offset = sizeof(int) + sizeof(int);

            foreach (var sb in stateBehaviours)
            {
                sb.Ptr = ptr + offset;

                offset += sb.Size;
            }
        }

        internal void ClearPointer()
        {
            Ptr = null;
        }

        public StateBehaviour StateBehaviourContainingPointer(byte* ptr)
        {
            foreach (var sb in stateBehaviours)
            {
                if (ptr >= sb.Ptr && ptr < sb.Ptr + sb.Size)
                    return sb;
            }

            return null;
        }

        public override void TickStart()
        {
            foreach (var tb in tickBehaviours)
            {
                tb.TickStart();
            }
        }

        public override void TickDestroy()
        {
            foreach (var tb in tickBehaviours)
            {
                tb.TickDestroy();
            }
        }

        public override void TickUpdate()
        {
            foreach (var tb in tickBehaviours)
            {
                tb.TickUpdate();
            }
        }

        public override void Render()
        {
            foreach (var tb in tickBehaviours)
            {
                tb.Render();
            }
        }

        public override void RenderStart()
        {
            foreach (var tb in tickBehaviours)
            {
                tb.RenderStart();
            }
        }
    }
}
