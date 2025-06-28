using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[Export] public float Speed = 5.0f;
	[Export] public float JumpVelocity = 8f;
	[Export] public float rotationSpeed = 5f;
	[Export] bool wasdControlEnabled = true;

	Node3D playerModel;
	PlayerCamera firstPersonCamera;

	Vector2 inputDirection = Vector2.Zero;
	Vector3 movementDirection = Vector3.Zero;
	Vector3 smoothedDirection = Vector3.Zero;

	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle() * 2.5f;

	public override void _Ready()
	{
		if(IsInstanceValid(GetNode<PlayerCamera>("PlayerCamera")))
		{
			firstPersonCamera = GetNode<PlayerCamera>("PlayerCamera");
		}

		if(IsInstanceValid(GetNode<MeshInstance3D>("PlayerModel")))
		{
			playerModel = GetNode<MeshInstance3D>("PlayerModel");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		GetInput();
		Move();
		Rotate();
	}

	private void GetInput()
	{
		// Get inputs
		if(Input.GetVector("move_left", "move_right", "move_forward", "move_back").LengthSquared() > 0)
		{
			inputDirection = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		}
		else if(wasdControlEnabled && (Input.IsPhysicalKeyPressed(Key.W) || Input.IsPhysicalKeyPressed(Key.A) || Input.IsPhysicalKeyPressed(Key.S) || Input.IsPhysicalKeyPressed(Key.D)))
		{
			inputDirection = new Vector2(Convert.ToInt32(Input.IsPhysicalKeyPressed(Key.D)) - Convert.ToInt32(Input.IsPhysicalKeyPressed(Key.A)),
			Convert.ToInt32(Input.IsPhysicalKeyPressed(Key.S)) - Convert.ToInt32(Input.IsPhysicalKeyPressed(Key.W)));
		}
		else
		{
			if(Mathf.Abs(Input.GetJoyAxis(0, JoyAxis.LeftX)) >= 0.5) inputDirection.X = Input.GetJoyAxis(0, JoyAxis.LeftX);
			else inputDirection.X = 0;
			
			if(Mathf.Abs(Input.GetJoyAxis(0, JoyAxis.LeftY)) >= 0.5) inputDirection.Y = Input.GetJoyAxis(0, JoyAxis.LeftY);
			else inputDirection.Y = 0;
		}
	}

	private void Move()
	{
		Vector3 velocity = Velocity;

		// Add the gravity
		if (!IsOnFloor()) velocity.Y -= gravity * (float)GetPhysicsProcessDeltaTime();

		// Handle Jump
		if ((Input.IsPhysicalKeyPressed(Key.Space) || Input.IsJoyButtonPressed(0, JoyButton.A)) && IsOnFloor()) velocity.Y = JumpVelocity;

		// Convert input to movementDirection
		movementDirection = (Transform.Basis * new Vector3(inputDirection.X, 0, inputDirection.Y)).Normalized();

		// Rotate movement movementDirection towards camera movementDirection
		if(IsInstanceValid(firstPersonCamera))
		{
			movementDirection = movementDirection.Rotated(Vector3.Up, firstPersonCamera.Rotation.Y);
		}

		// Smooth movement
		smoothedDirection = smoothedDirection.Lerp(movementDirection, 18f * (float)GetPhysicsProcessDeltaTime());

		// Apply movement
		if (smoothedDirection != Vector3.Zero)
		{
			velocity.X = smoothedDirection.X * Speed;
			velocity.Z = smoothedDirection.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}
		Velocity = velocity;
		MoveAndSlide();

	}

	private void Rotate()
	{
		// Rotate player
		if(IsInstanceValid(playerModel))
		{
			float yRotation = Mathf.LerpAngle(playerModel.Rotation.Y, Mathf.Atan2(firstPersonCamera.GlobalBasis.Z.X, firstPersonCamera.GlobalBasis.Z.Z), rotationSpeed * (float)GetPhysicsProcessDeltaTime());
			Vector3 rotationSmoothed = new Vector3(playerModel.Rotation.X, yRotation, playerModel.Rotation.Z);
			playerModel.Rotation = rotationSmoothed;
		}
	}
}
