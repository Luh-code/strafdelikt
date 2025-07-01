using Godot;
using System;

public partial class Viewmodel : SubViewportContainer
{
	[Export] SubViewport viewport;
	
	public override void _Ready()
	{
		//viewport.Size = DisplayServer.ScreenGetSize();
	}
}
