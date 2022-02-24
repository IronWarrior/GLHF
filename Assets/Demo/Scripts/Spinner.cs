using GLHF;
using UnityEngine;

public unsafe class Spinner : StateBehaviour
{
    [SerializeField]
    float speed = 5;

    public float Angle
    {
        get => *(float*)(Ptr);
        set => *(float*)(Ptr) = value;
    }

    public override int Size => sizeof(float);

    public override void TickUpdate()
    {
        Angle += speed * Runner.DeltaTime;

        if (Angle > 360)
            Angle -= 360;
    }

    public override void Render()
    {
        transform.rotation = Quaternion.Euler(0, Angle, 0);
    }
}
