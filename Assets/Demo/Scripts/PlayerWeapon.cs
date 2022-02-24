using GGEZ;
using UnityEngine;

public unsafe class PlayerWeapon : StateBehaviour
{
    [SerializeField]
    float cooldown = 1;

    [SerializeField]
    Projectile projectilePrefab;

    public float LastFireTime
    {
        get => *(float*)(Ptr);
        set => *(float*)(Ptr) = value;
    }

    public override int Size => sizeof(float);

    public override void TickUpdate()
    {
        var input = Runner.GetInput(GetComponent<PlayerPhysics>().Player);

        if (input.Fire && Runner.Time > LastFireTime + cooldown)
        {
            var projectile = Runner.Spawn(projectilePrefab, GetComponent<StateTransform>().Position);
            projectile.Direction = new Vector3(1, 0, 0);

            LastFireTime = Runner.Time;
        }
    }
}
