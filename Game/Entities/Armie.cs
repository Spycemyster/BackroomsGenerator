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
    public float ChaseSpeed = 10f;
    public float PatrolSpeed = 5f;
    private Vector3 mVelocity = Vector3.Zero;
    private ArmieState mState;
    private Vector3 mLastPositionSeen;
    private AudioStreamPlayer3D mSFX;
    private RayCast mPlayerDetector;
    private const float DETECTION_THRESHOLD_DOT = -0.5f;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        mPlayerDetector = GetNode<RayCast>("PlayerDetector");
        mSFX = GetNode<AudioStreamPlayer3D>("AudioStreamPlayer3D");
        Player = GetTree().Root.GetNode<Player>("World/Player");
        mState = ArmieState.IDLE;
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

        if (state != ArmieState.CHASE)
        {
            mSFX.Playing = false;
        }
        else
        {
            mSFX.Play(0);
        }
    }

    private bool canDetectPlayer()
    {
        mPlayerDetector.CastTo = Player.GlobalTransform.origin - GlobalTransform.origin;
        mPlayerDetector.Rotation = -Rotation;
        mPlayerDetector.ForceRaycastUpdate();

        return mPlayerDetector.GetCollider() == Player;
    }

    private void updatePatrol(float dt)
    {

    }

    private void updateIdle(float dt)
    {
        Vector3 ray = -GlobalTransform.basis.z;
        
        Vector3 toPlayer = (Player.GlobalTransform.origin - GlobalTransform.origin).Normalized();

        float d = ray.Dot(toPlayer);
        
        // we have found the player
        if (d > DETECTION_THRESHOLD_DOT && canDetectPlayer())
        {
            ChangeState(ArmieState.CHASE);
        }
    }

    private float mUnseenTimer = 0f;
    private const float LOST_TIMER = 5f;
    private void updateChase(float dt)
    {
        Vector3 ray = -GlobalTransform.basis.z;
        Vector3 toPlayer = (Player.GlobalTransform.origin - GlobalTransform.origin);
        float d = ray.Dot(toPlayer.Normalized());

        if (d < DETECTION_THRESHOLD_DOT || toPlayer.LengthSquared() > 1000 || !canDetectPlayer())
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
        LookAt(mLastPositionSeen, Vector3.Up);
        Vector3 rot = Rotation;
        rot.x = 0;
        rot.z = 0;
        Rotation = rot;
        Vector3 vel = new Vector3(-Mathf.Sin(rot.y), 0, -Mathf.Cos(rot.y)) * ChaseSpeed;
        MoveAndSlide(vel, Vector3.Up);
    }
}
