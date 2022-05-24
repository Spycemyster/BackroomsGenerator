using Godot;
using System;

public class PauseHandler : Node
{


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        PauseMode = Node.PauseModeEnum.Process;
    }

    public override void _Process(float delta)
    {
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (Input.IsActionJustPressed("pause"))
        {
            GetTree().Paused = !GetTree().Paused;
        }
        if (GetTree().Paused)
            Input.SetMouseMode(Input.MouseMode.Visible);
        else
            Input.SetMouseMode(Input.MouseMode.Captured);
    }
}
