using Godot;

public partial class PlayerCamera : Node3D
{
	[Export] Camera3D camera;

	[Export] Vector3 cameraOffset = new Vector3(0, 0.5f, 0);
	[Export] float mouseSensitivity = 8f;
	[Export] float controllerSensitivity = 3f;
	[Export] float clampUpAt = 80;
	[Export] float clampDownAt = 80;
	[Export] float controllerCameraStiffness = 20f;

	Vector2 mouseInput;
	private Vector2 smoothedControllerInput = Vector2.Zero;
	
	public bool debug = false;

	public override void _Ready()
	{
		if(IsInstanceValid(GetNode<Camera3D>("Camera3D")))
		{
			camera = GetNode<Camera3D>("Camera3D");
			camera.Current = true;
		}

		Position += cameraOffset;
		DisplayServer.MouseSetMode(DisplayServer.MouseMode.Captured);
	}

	public override void _Input(InputEvent @event)
	{
		if(@event is InputEventMouseMotion mouseEvent)
		{	
			mouseInput = mouseEvent.Relative;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		//Escp to quit
		if(Input.IsPhysicalKeyPressed(Key.Escape))
		{
			GetTree().Quit();
		}
		
		if (Input.IsActionJustPressed("debug_key"))
		{
			if (!debug)
				DisplayServer.MouseSetMode(DisplayServer.MouseMode.Visible);
			else
				DisplayServer.MouseSetMode(DisplayServer.MouseMode.Captured);
			debug = !debug;
		}
		
		RotateCamera();
	}

	private void RotateCamera()
	{
		// Rotates camera either by mouse or by secondary controller analog stick
		// If both devices are used simultaneously, mouse has priority
		
		Vector3 newRotationDegrees = RotationDegrees;
		Vector3 newCameraRotationDegrees = camera.RotationDegrees;

		if(mouseInput != Vector2.Zero && !debug)
		{
			newCameraRotationDegrees.X -= mouseInput.Y * mouseSensitivity * (1920 / DisplayServer.WindowGetSize().X * (float)GetPhysicsProcessDeltaTime());
			newCameraRotationDegrees.X = Mathf.Clamp(newCameraRotationDegrees.X, -80, 80);

			newRotationDegrees.Y -= mouseInput.X * mouseSensitivity * (1080 / DisplayServer.WindowGetSize().Y * (float)GetPhysicsProcessDeltaTime());
			newRotationDegrees.Y = Mathf.Wrap(newRotationDegrees.Y, 0, 360);

			RotationDegrees = newRotationDegrees;
			camera.RotationDegrees = newCameraRotationDegrees;
			mouseInput = Vector2.Zero;
		}
		Vector2 controllerInput = Vector2.Zero;
		if (!debug)
		{
			if(Mathf.Abs(Input.GetJoyAxis(0, JoyAxis.RightX)) >= 0.5) controllerInput.X = -Input.GetJoyAxis(0, JoyAxis.RightX);
			else controllerInput.X = 0;
			
			if(Mathf.Abs(Input.GetJoyAxis(0, JoyAxis.RightY)) >= 0.5) controllerInput.Y = Input.GetJoyAxis(0, JoyAxis.RightY);
			else controllerInput.Y = 0;

			smoothedControllerInput = smoothedControllerInput.Lerp(controllerInput, controllerCameraStiffness * (float)GetPhysicsProcessDeltaTime());
			newCameraRotationDegrees.X -= smoothedControllerInput.Y * controllerSensitivity;
			newCameraRotationDegrees.X = Mathf.Clamp(newCameraRotationDegrees.X, -clampUpAt, clampDownAt);

			newRotationDegrees.Y += smoothedControllerInput.X * controllerSensitivity;
			newRotationDegrees.Y = Mathf.Wrap(newRotationDegrees.Y, 0, 360);

			RotationDegrees = newRotationDegrees;
			camera.RotationDegrees = newCameraRotationDegrees * System.Convert.ToSingle(!debug);
		}
	}
}
