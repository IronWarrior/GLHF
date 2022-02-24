using UnityEngine;
using GGEZ;

public unsafe class PlayerPhysics : StateBehaviour
{
    [SerializeField]
    float speed = 1;

    public bool IsRed
    {
        get => *(bool*)Ptr;
        set => *(bool*)Ptr = value;
    }

    public int Player
    {
        get => *(int*)(Ptr + 1);
        set => *(int*)(Ptr + 1) = value;
    }

    public override int Size => sizeof(bool) + sizeof(int);

    private InputActions actions;

    private StateInput OnPollInput()
    {
        StateInput input = new StateInput();

        var moveInput = actions.Default.Move.ReadValue<Vector2>();
        input.MoveDirection = new Vector3(moveInput.x, 0, moveInput.y);
        input.Fire = actions.Default.Fire.ReadValue<float>() > 0.5f;

        return input;
    }

    public override void TickStart()
    {
        actions = new InputActions();
        actions.Enable();

        Runner.OnPollInput = OnPollInput;
    }

    public override void TickUpdate()
    {
        if (Runner.Tick % 10 == 0)
        {
            IsRed = !IsRed;
        }

        StateInput input = Runner.GetInput(Player);

        var t = GetComponent<StateTransform>();

        t.Position += speed * input.MoveDirection * Runner.DeltaTime;
    }

    public void SetPlayerIndex(int index)
    {
        Player = index;
    }
}
