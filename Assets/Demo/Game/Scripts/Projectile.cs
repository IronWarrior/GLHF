using GLHF;
using UnityEngine;
using Unity.Mathematics;

public unsafe class Projectile : StateBehaviour
{
    [SerializeField]
    float speed = 5;

    [SerializeField]
    float lifetime = 5;

    public float3 Direction
    {
        get => *(float3*)(Ptr);
        set => *(float3*)(Ptr) = value;
    }

    public float LifetimeStart
    {
        get => *(float*)(Ptr + sizeof(float3));
        set => *(float*)(Ptr + sizeof(float3)) = value;
    }

    public override int Size => sizeof(float3) + sizeof(float);

    public override void TickStart()
    {
        LifetimeStart = Simulation.Time;
    }

    public override void TickUpdate()
    {
        GetComponent<StateTransform>().Position += speed * Direction * Simulation.DeltaTime;

        if (Simulation.Time > LifetimeStart + lifetime)
        {
            Simulation.Despawn(Object);
        }
    }
}
