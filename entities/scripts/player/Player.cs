using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[Export] public float Speed = 8.0f;
	[Export] public float SprintSpeed = 15.0f;
	[Export] public float JumpVelocity = 10f;
	[Export] public float rotationSpeed = 6f;
	[Export] public float AccelSpeed = 2.0f;
	[Export] public float DecelSpeed = 4.0f;
	[Export] public float LateralCofactor = 2.5f;
	[Export] public float LateralCofactor2 = 0.3f;
	[Export] public Curve AccelCurve;
	[Export] public Curve DecelCurve;
	[Export] public DebugUi debugUI;
	[Export] public Node2D VArm;
	[Export] public Node2D DArm;
	[Export] public Node2D AArm;
	//[Export] public Curve AccelJerk;
	//[Export] public float AccelTime = 2.0f;
	//[Export] public Curve DecelJerk;
	//[Export] public float DecelTime = 1.0f;

	Node3D playerModel;
	public PlayerCamera firstPersonCamera;

	Vector2 inputDirection = Vector2.Zero;
	Vector3 movementDirection = Vector3.Zero;
	Vector3 smoothedDirection = Vector3.Zero;
	//Vector3 acceleration = Vector3.Zero;
	bool isAccelerating = false;
	bool isDecelerating = false;
	long accelTime = 0;
	long decelTime = 0;


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
		Move(delta);
		Rotate();
	}

	private void GetInput()
	{
		// Get inputs
		Vector2 inputVector = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		
		if(inputVector.Y != 0 && !isAccelerating)
		{
			isAccelerating = true;
			isDecelerating = false;
			accelTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
		}
		else if(inputVector.Y == 0 && isAccelerating)
		{
			isAccelerating = false;
			isDecelerating = true;
			decelTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
			//long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
			//float jerkProgress = Mathf.Min((currentTime-accelTime)/1000.0f, 1.0f);
			//decelTime = 
		}
		
		if(inputVector.LengthSquared() > 0)
		{
			inputDirection = inputVector;
		}
		else
		{
			if(Mathf.Abs(Input.GetJoyAxis(0, JoyAxis.LeftX)) >= 0.5) inputDirection.X = Input.GetJoyAxis(0, JoyAxis.LeftX);
			else inputDirection.X = 0;
			
			if(Mathf.Abs(Input.GetJoyAxis(0, JoyAxis.LeftY)) >= 0.5) inputDirection.Y = Input.GetJoyAxis(0, JoyAxis.LeftY);
			else inputDirection.Y = 0;
		}
		
		
	}

	public void _UndhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey eventKey)
		{
			//if(eventKey)
		}
	}

	private void Move(double delta)
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
		long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
		float accelProgress = Mathf.Min((currentTime-accelTime)/1000.0f, 1.0f);
		float decelProgress = Mathf.Min((currentTime-decelTime)/1000.0f, 1.0f);
		
		//if (isAccelerating)
		//{
		//	if (jerkProgress == 1.0f)
		//	{
		//		acceleration = Vector3.Zero;
		//	}
		//	acceleration.X += smoothedDirection.X * AccelJerk.Sample(jerkProgress) * (float) delta;
		//	acceleration.Z += smoothedDirection.Z * AccelJerk.Sample(jerkProgress) * (float) delta;
		//}
		//else if (isDecelerating)
		//{
		//	Vector3 accelDirection = acceleration.Normalized();
		//	acceleration.X -= accelDirection.X * DecelJerk.Sample(1.0f-jerkProgress) * (float) delta;
		//	acceleration.Z -= accelDirection.Z * DecelJerk.Sample(1.0f-jerkProgress) * (float) delta;
		//	if (jerkProgress == 1.0f)
		//	{
		//		acceleration = Vector3.Zero;
		//	}
		//}
		
		//velocity += acceleration * new Vector3((float)delta, (float)delta, (float)delta);
		//vGD.Print(acceleration);
		
		Vector3 velocityXZ = new Vector3(velocity.X, 0, velocity.Z);
		
		// Calculate deceleration force
		float decelStrength = DecelCurve.SampleBaked(decelProgress);
		//Vector3 deceleration = velocityXZ.Normalized() * decelStrength * DecelSpeed * (float) delta;
		float deceleration = decelStrength * DecelSpeed * (float) delta;
		//if (velocityXZ.Length() - deceleration.Length() < 0)
		//{
		//	deceleration = deceleration.Normalized() * (velocityXZ.Length());
		//}
		if (velocityXZ.Length() - deceleration < 0)
		{
			deceleration = velocityXZ.Length();
		}
		
		float actingSpeed = (Input.IsActionPressed("move_sprint") ? SprintSpeed : Speed);
		if (movementDirection != Vector3.Zero && !firstPersonCamera.debug)// && velocity.Length() < Speed)
		{
			// Accelerate
			float accelStrength = AccelCurve.SampleBaked(accelProgress);
			Vector3 acceleration = (smoothedDirection * accelStrength * AccelSpeed * (float) delta);
			if ((velocityXZ + acceleration).Length() > actingSpeed)
			{
				//acceleration = Vector3.Zero;
				acceleration = acceleration.Normalized() * (actingSpeed-velocityXZ.Length());
			}
			velocity += acceleration;
			
			// Calclate deceleration
			Vector2 v = new Vector2(velocity.X, velocity.Z).Normalized();
			Vector2 a = new Vector2(acceleration.X, acceleration.Z).Normalized();
			//float c = Mathf.Abs(1-v.Dot(a)); //Mathf.Min(Mathf.Abs(1-v.Dot(a)), 1.0f);
			float c = Mathf.Min(Mathf.Abs(1-v.Dot(a)), 1.0f);
			//GD.Print("v: " + v + " a: " + a + " c: " + c);
			float avAngle = Mathf.Atan2((a.Y*v.X) - (a.X*v.Y), (a.Y*v.Y) + (a.X*v.X));
			Vector3 d = -(new Vector3(v.X, 0, v.Y).Normalized().Rotated(Vector3.Up, avAngle)) * deceleration * c * LateralCofactor;
			d = d * -(Mathf.Pow(velocity.Length(), LateralCofactor2)*new Vector2(d.X, d.Z).Normalized().Dot(v));
			//d = d * Mathf.Pow(d.Length(), 0.5f);
			//d = d.Rotated(Vector3.Up, avAngle);
			
			velocity += d;
			
			velocityXZ = new Vector3(velocity.X, 0, velocity.Z);
			
			Vector2 right = new Vector2(1, 0);
			Vector2 dXZ = new Vector2(d.X, d.Z);
			//VArm.SetRotation((Mathf.Abs(v.Dot(right))/(v.Length()*right.Length()))*4);
			//DArm.SetRotation((Mathf.Abs(dXZ.Dot(right))/(dXZ.Length()*right.Length()))*4);
			//AArm.SetRotation((Mathf.Abs(a.Dot(right))/(a.Length()*right.Length()))*6);
			debugUI.v = velocity;
			debugUI.a = acceleration;
			debugUI.c = c;
			debugUI.avAngle = Mathf.RadToDeg(avAngle);
			VArm.SetRotation(MathF.Atan2(v.Y, v.X));
			DArm.SetRotation(MathF.Atan2(dXZ.Y, dXZ.X));
			DArm.SetScale(new Vector2(d.Length()/deceleration, DArm.GetScale().Y)*(debugUI.ShowBreakAsForce.ButtonPressed ? -1 : 1));
			AArm.SetRotation(MathF.Atan2(a.Y, a.X));
		}
		else
		{
			velocityXZ = new Vector3(velocity.X, 0, velocity.Z);
			// Decelerate
			
			velocity -= velocityXZ.Normalized() * deceleration;
		}
		
		
		
		Velocity = velocity;
		//GD.Print(velocity.Length());
		//GD.Print(velocity);
		
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
