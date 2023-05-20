using GLHF;
using UnityEngine;

public class InputHandler : MonoBehaviour, IInputHandler
{
    private InputActions actions;

    private void Awake()
    {
        actions = new InputActions();
        actions.Enable();
    }

    private void OnDestroy()
    {
        actions.Dispose();
    }

    public StateInput GetInput()
    {
        StateInput input = new();

        var moveInput = actions.Default.Move.ReadValue<Vector2>();
        input.MoveDirection = new Vector3(moveInput.x, 0, moveInput.y);
        input.Fire = actions.Default.Fire.ReadValue<float>() > 0.5f;

        return input;
    }
}
