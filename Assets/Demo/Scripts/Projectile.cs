using GGEZ;
using UnityEngine;

public unsafe class Projectile : StateBehaviour
{
    [SerializeField]
    float speed = 5;

    [SerializeField]
    float lifetime = 5;

    public Vector3 Direction
    {
        get => *(Vector3*)(Ptr);
        set => *(Vector3*)(Ptr) = value;
    }

    public float LifetimeStart
    {
        get => *(float*)(Ptr + sizeof(Vector3));
        set => *(float*)(Ptr + sizeof(Vector3)) = value;
    }

    public override int Size => sizeof(Vector3) + sizeof(float);

    public override void TickStart()
    {
        LifetimeStart = Runner.Time;
    }

    public override void TickUpdate()
    {
        GetComponent<StateTransform>().Position += speed * Direction * Runner.DeltaTime;

        if (Runner.Time > LifetimeStart + lifetime)
        {
            Runner.Despawn(Object);
        }
    }
}
