using Godot;
using System;

public partial class DebugUi : Control
{
	[ExportSubgroup("Main")]
	[Export] public Player player;
	[Export] public Control ClockWidgetOptions;
	[Export] public CheckBox ShowBreakAsForce;
	[Export] public Label vValue;
	[Export] public Label aValue;
	[Export] public Label vnValue;
	[Export] public Label anValue;
	[Export] public Label cValue;
	[Export] public Label avAngleValue;
	[Export] public Label speedValue;
	
	[ExportSubgroup("Clock")]
	[Export] public Node2D AArm;
	[Export] public Node2D VArm;
	[Export] public Node2D DArm;
	[Export] public Node2D MArm;
	
	public Vector3 v = Vector3.Zero;
	public Vector3 a = Vector3.Zero;
	public float c = 0.0f;
	public float avAngle = 0.0f;
	public float speedP = 0.0f;
	
	public void _on_a_enabled_toggled(bool toggledOn)
	{
		AArm.SetVisible(toggledOn);
	}
	
	public void _on_v_enabled_toggled(bool toggledOn)
	{
		VArm.SetVisible(toggledOn);
	}
	
	public void _on_d_enabled_toggled(bool toggledOn)
	{
		DArm.SetVisible(toggledOn);
	}
	
	public void _on_m_enabled_toggled(bool toggledOn)
	{
		MArm.SetVisible(toggledOn);
	}
	
	public override void _Process(double delta)
	{
		ClockWidgetOptions.SetVisible(player.firstPersonCamera.debug);
		vValue.Text = v.ToString();
		vnValue.Text = v.Normalized().ToString();
		aValue.Text = a.ToString();
		anValue.Text = a.Normalized().ToString();
		cValue.Text = c.ToString();
		avAngleValue.Text = avAngle.ToString();
		speedValue.Text = ((int)(speedP*100)).ToString();
	}
}
