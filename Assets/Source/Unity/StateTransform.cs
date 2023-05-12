using UnityEngine;

namespace GLHF
{
    public unsafe class StateTransform : StateBehaviour
    {
        public Vector3 Position
        {
            get => *(Vector3*)Ptr;

            set
            {
                *(Vector3*)Ptr = value;
            }
        }

        public override int Size => sizeof(Vector3);

        public override void Render()
        {
            transform.position = Position;
        }
    }
}
