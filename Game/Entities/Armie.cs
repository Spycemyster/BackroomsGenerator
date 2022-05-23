using Godot;
using System;

public class Armie : KinematicBody
{
    private enum ArmieState
    {
        IDLE,
        PATROL,
        CHASE,
    }
    public Player Player;
    private RayCast mRaycast;
    public float ChaseSpeed = 10f;
    public float PatrolSpeed = 5f;
    private Vector3 mVelocity = Vector3.Zero;
    private ArmieState mState;
    private Vector3 mLastPositionSeen;
    private Spatial mBody;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        mBody = GetNode<Spatial>("BodyMesh");
        Player = GetTree().Root.GetNode<Player>("World/Player");
        mState = ArmieState.IDLE;
        mRaycast = GetNode<RayCast>("RayCast");
    }

    public override void _PhysicsProcess(float delta)
    {
        base._PhysicsProcess(delta);
        mVelocity.y -= Globals.GRAVITY * delta;
        mVelocity = MoveAndSlide(mVelocity, Vector3.Up);

        switch (mState)
        {
            case ArmieState.IDLE:
                updateIdle(delta);
                break;
            case ArmieState.CHASE:
                updateChase(delta);
                break;
            case ArmieState.PATROL:
                updatePatrol(delta);
                break;
        }
    }

    private void ChangeState(ArmieState state)
    {
        mState = state;
        GD.Print($"Changed to state {state}");
    }

    private void updatePatrol(float dt)
    {

    }

    private void updateIdle(float dt)
    {
        Vector3 ray = GetForward();
        Vector3 toPlayer = Player.GlobalTransform.origin.DirectionTo(GlobalTransform.origin);

        // we have found the player
        if (Math.Abs(ray.Dot(toPlayer)) > 0.9f)
        {
            ChangeState(ArmieState.CHASE);
        }
    }

    private float mUnseenTimer = 0f;
    private const float LOST_TIMER = 5f;
    private void updateChase(float dt)
    {

        if (mRaycast.GetCollider() != Player)
        {
            // player is not within our range
            mUnseenTimer += dt;

            // haven't been seen for LOST_TIMER amount of time, then we go to patrol mode
            if (mUnseenTimer > LOST_TIMER)
            {
                ChangeState(ArmieState.IDLE);
                mUnseenTimer = 0f;
            }
        }
        else
        {
            mLastPositionSeen = Player.GlobalTransform.origin;
        }
        mBody.LookAt(mLastPositionSeen, Vector3.Up);
        Vector3 rot = mBody.Rotation;
        rot.x = 0;
        rot.z = 0;
        mBody.Rotation = rot;
    }

    public Vector3 GetForward()
    {
        return new Vector3(-Mathf.Sin(mBody.Rotation.y), 0, Mathf.Cos(mBody.Rotation.y));
    }
}
