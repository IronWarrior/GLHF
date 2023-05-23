using Unity.Mathematics;

namespace GLHF
{
    public unsafe class StateTransform : StateBehaviour
    {
        public float3 Position
        {
            get => *(float3*)Ptr;

            set
            {
                *(float3*)Ptr = value;
            }
        }

        public override int Size => sizeof(float3);

        public override void Render()
        {
            transform.position = Position;
        }
    }
}
