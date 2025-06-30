using Godot;
using System;

public partial class DebugUi : Control
{
	[Export] public Player player;
	[Export] public Control ClockWidgetOptions;
	[Export] public CheckBox ShowBreakAsForce;
	
	public override void _PhysicsProcess(double delta)
	{
		ClockWidgetOptions.SetVisible(player.firstPersonCamera.debug);
	}
}
