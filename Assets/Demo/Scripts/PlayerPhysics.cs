using UnityEngine;
using GLHF;

public unsafe class PlayerPhysics : StateBehaviour
{
    [SerializeField]
    float speed = 1;

    public int Player
    {
        get => *(int*)Ptr;
        set => *(int*)Ptr = value;
    }

    public override int Size => sizeof(bool) + sizeof(int);

    public override void TickUpdate()
    {
        StateInput input = Simulation.GetInput(Player);

        var t = GetComponent<StateTransform>();

        t.Position += speed * input.MoveDirection * Simulation.DeltaTime;
    }

    public void SetPlayerIndex(int index)
    {
        Player = index;
    }
}
