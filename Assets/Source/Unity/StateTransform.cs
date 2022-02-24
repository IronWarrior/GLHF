using UnityEngine;

namespace GGEZ
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

        // TODO: This is not deterministic, should be replaced by saving at edit time.
        //public override void TickStart()
        //{
        //    Position = new fp3(transform.position);
        //}

        public override void Render()
        {
            transform.position = Position;
        }
    }
}
