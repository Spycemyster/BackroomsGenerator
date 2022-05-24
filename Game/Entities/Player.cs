using Godot;
using System;

public class Player : KinematicBody
{
    [Export]
    /// <summary>
    /// The maximum movement speed of the player.
    /// </summary>
    public float Movespeed = 8f;

    [Export]
    /// <summary>
    /// Sprint multiplier
    /// </summary>
    public float SprintMultiplier = 2.0f;

    [Export]
    /// <summary>
    /// Mouse sensitivity.
    /// </summary>
    public float MouseSensitivity = 0.025f;

    [Export]
    /// <summary>
    /// Base jump magnitude
    /// </summary>
    public float BaseJumpMagnitude = 12f;
    private Camera mCamera;
    private Vector3 mVelocity;
    private const float MIN_ANGLE = -90.0f;
    private const float MAX_ANGLE = 90.0f;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        mCamera = GetNode<Camera>("Camera");
        mVelocity = Vector3.Zero;
    }

    public override void _PhysicsProcess(float delta)
    {
        base._PhysicsProcess(delta);

        // player controlled movement
        // we project controlled movement from WASD onto the XZ-Plane
        Vector3 forward = GlobalTransform.basis.z;
        Vector3 right = GlobalTransform.basis.x;

        Vector3 input = new Vector3(Input.GetActionStrength("move_backward") - Input.GetActionStrength("move_forward"),
            0, Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left"));
        
        bool isSprinting = Input.IsActionPressed("sprint");
        if (Input.IsActionPressed("jump") && IsOnFloor())
        {
            Jump();
        }

        if (Input.IsActionJustPressed("force_quit"))
        {
            GetTree().Quit(0);
        }
        input = input.Normalized();

        // maps the input towards the correct movement direction
        Vector3 relativeDir = forward * input.x + right * input.z;
        if (IsOnFloor())
        {
            mVelocity.x = relativeDir.x * Movespeed * (isSprinting ? SprintMultiplier : 1f);
            mVelocity.z = relativeDir.z * Movespeed * (isSprinting ? SprintMultiplier : 1f);
        }
        mVelocity.y -= Globals.GRAVITY * delta;

        mVelocity = MoveAndSlide(mVelocity, Vector3.Up);
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event is InputEventMouseMotion mouse)
        {
            // horizontal looking (Swiping mouse left and right)
            Vector3 cameraRotation = mCamera.RotationDegrees;
            cameraRotation.x -= mouse.Relative.y * MouseSensitivity;
            cameraRotation.x = Mathf.Clamp(cameraRotation.x, MIN_ANGLE, MAX_ANGLE);
            mCamera.RotationDegrees = cameraRotation;

            // vertical looking (Swiping mouse up and down)
            Vector3 bodyRotation = RotationDegrees;
            bodyRotation.y -= mouse.Relative.x * MouseSensitivity;
            RotationDegrees = bodyRotation;
        }
    }

    /// <summary>
    /// Gives the player a jump boost.
    /// </summary>
    public void Jump()
    {
        mVelocity.y += BaseJumpMagnitude;
    }
}
