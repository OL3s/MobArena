using Godot;

namespace MobArena.Scripts.Resources;

[GlobalClass]
public partial class ArenaCombatInputState : Resource
{
    private const float DefaultMoveDeadzone = 0.3f;
    private Vector2 _moveInput = Vector2.Zero;

    [Export]
    public Vector2 MoveDirection { get; private set; } = Vector2.Zero;

    [Export]
    public float MoveStrength { get; private set; }

    [Export]
    public bool IsMoving { get; private set; }

    [Export(PropertyHint.Range, "0,0.95,0.01")]
    public float MoveDeadzone { get; private set; } = DefaultMoveDeadzone;

    [Export]
    public bool MainHandPressed { get; private set; }

    [Export]
    public bool OffHandPressed { get; private set; }

    [Export]
    public bool AbilityPressed { get; private set; }

    [Export]
    public bool BlockPressed { get; private set; }

    public void SetMoveDirection(Vector2 moveDirection)
    {
        _moveInput = moveDirection;
        ApplyMoveInput();
    }

    public void SetMoveDeadzone(float moveDeadzone)
    {
        MoveDeadzone = Mathf.Clamp(moveDeadzone, 0f, 0.95f);
        ApplyMoveInput();
    }

    private void ApplyMoveInput()
    {
        var clampedInput = _moveInput.LengthSquared() > 1f
            ? _moveInput.Normalized()
            : _moveInput;
        var rawStrength = Mathf.Clamp(clampedInput.Length(), 0f, 1f);
        var deadzone = Mathf.Clamp(MoveDeadzone, 0f, 0.95f);

        MoveStrength = rawStrength <= deadzone
            ? 0f
            : Mathf.Clamp((rawStrength - deadzone) / (1f - deadzone), 0f, 1f);
        MoveDirection = MoveStrength > 0f && clampedInput.LengthSquared() > 0f
            ? clampedInput.Normalized()
            : Vector2.Zero;
        IsMoving = MoveDirection != Vector2.Zero;
    }

    public void SetActionPressed(bool mainHandPressed, bool offHandPressed, bool abilityPressed, bool blockPressed)
    {
        MainHandPressed = mainHandPressed;
        OffHandPressed = offHandPressed;
        AbilityPressed = abilityPressed;
        BlockPressed = blockPressed;
    }

    public void ClearActions()
    {
        MainHandPressed = false;
        OffHandPressed = false;
        AbilityPressed = false;
        BlockPressed = false;
    }

    public void Reset()
    {
        _moveInput = Vector2.Zero;
        MoveDirection = Vector2.Zero;
        MoveStrength = 0f;
        IsMoving = false;
        ClearActions();
    }
}
