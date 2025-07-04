using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[ExportSubgroup("Main")]
	[Export(PropertyHint.Range, "0, 10")] public float Mass = 1.0f;
	[Export(PropertyHint.Range, "0, 50")] public float Speed = 8.0f;
	[Export(PropertyHint.Range, "0, 50")] public float SprintSpeed = 15.0f;
	[Export(PropertyHint.Range, "0, 20")] public float JumpVelocity = 10f;
	[Export(PropertyHint.Range, "0, 20")] public float JumpSpeedBoost = 4.0f;
	[Export(PropertyHint.Range, "0, 10")] public float rotationSpeed = 6f;
	[Export(PropertyHint.Range, "0, 300")] public float AccelSpeed = 2.0f;
	[Export(PropertyHint.Range, "0, 300")] public float DecelSpeed = 4.0f;
	[Export(PropertyHint.Range, "0, 300")] public float AirDecelSpeed = 3.0f;
	[Export(PropertyHint.Range, "1, 10")] public float LateralCofactor = 4.1f;
	[Export(PropertyHint.Range, "0, 1")] public float LateralCofactor2 = 0.3f;
	[Export(PropertyHint.Range, "0, 100")] public float DashVelocity = 48.0f;
	[Export] public Curve AccelCurve;
	[Export] public Curve DecelCurve;
	[Export] public Curve CounterAccelFactor;
	[Export(PropertyHint.Range, "0, 1")] public float VelocityRedirection = 0.2f;
	//[Export] public Curve AirResistanceCofactor;
	[ExportSubgroup("Nodes")]
	[Export] public Timer DashTimer; 
	[ExportSubgroup("HERE BE DRAGONS")]
	[Export(PropertyHint.Range, "0, 2")] public float DecelerationDirMod = 1.0f;
	[ExportSubgroup("Debug")]
	[Export] public DebugUi debugUI;
	[Export] public Node2D VArm;
	[Export] public Node2D DArm;
	[Export] public Node2D AArm;
	[Export] public Node2D MArm;

	Node3D playerModel;
	public PlayerCamera firstPersonCamera;

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
		Move(delta);
		Rotate();
	}

	private void GetInput()
	{
		// Get inputs
		Vector2 inputVector = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		
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
		
	}

	private void Move(double delta)
	{
		Vector3 velocity = Velocity;

		// Add the gravity
		if (!IsOnFloor()) velocity.Y -= gravity * Mass * (float)GetPhysicsProcessDeltaTime();

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
		
		// Determine values
		float actingSpeed = (Input.IsActionPressed("move_sprint") ? SprintSpeed : Speed);
		float actingDecelSpeed = !IsOnFloor() ? AirDecelSpeed : DecelSpeed;
		Vector3 velocityXZ = new Vector3(velocity.X, 0, velocity.Z);
		
		// Calculate redirection
		float theta = new Vector3(velocity.X, 0, velocity.Z).Normalized().AngleTo(new Vector3(smoothedDirection.X, 0, smoothedDirection.Z).Normalized());
		// GD.Print(theta);
		Vector3 redirectedVelocity = smoothedDirection * velocityXZ.Length();
		redirectedVelocity.Y = velocity.Y;
		velocity = velocity.Lerp(redirectedVelocity, VelocityRedirection);
		
		velocityXZ = new Vector3(velocity.X, 0, velocity.Z);
		
		// Calculate deceleration force
		float decelStrength = DecelCurve.SampleBaked(velocityXZ.Length()/actingSpeed);
		float deceleration = decelStrength * actingDecelSpeed * (float) delta;
		if (velocityXZ.Length() - deceleration < 0)
		{
			deceleration = velocityXZ.Length();
		}
		
		if (movementDirection != Vector3.Zero && !firstPersonCamera.debug)
		{
			// Accelerate
			float accelStrength = AccelCurve.SampleBaked(velocityXZ.Length()/actingSpeed);
			debugUI.speedP = velocityXZ.Length()/actingSpeed;
			Vector3 acceleration = (smoothedDirection * accelStrength * AccelSpeed * (float) delta);
			if ((velocityXZ + acceleration).Length() > actingSpeed)
			{
				if (velocityXZ.Length() > actingSpeed) {
					acceleration = Vector3.Zero;
				}
				else
				{
					var aDir = acceleration.Normalized();
					var aStren = Mathf.Min(Mathf.Max(actingSpeed-velocityXZ.Length(), 0), actingSpeed);//Mathf.Min(Mathf.Abs((actingSpeed-velocityXZ.Length())), actingSpeed);
					var aAttenuation = Mathf.Min(Mathf.Abs(1-((acceleration.Normalized()).Dot(velocityXZ.Normalized()))), 2.0f);
					
					acceleration = aDir * aStren * aAttenuation;
				}
			}
			velocity += acceleration;
			velocityXZ = new Vector3(velocity.X, 0, velocity.Z);
			
			
			// Calclate deceleration
			Vector2 v = new Vector2(velocity.X, velocity.Z).Normalized();
			Vector2 a = new Vector2(acceleration.X, acceleration.Z).Normalized();
			if (a == Vector2.Zero) a = new Vector2(smoothedDirection.X, smoothedDirection.Z);
			//float c = Mathf.Abs(1-v.Dot(a)); //Mathf.Min(Mathf.Abs(1-v.Dot(a)), 1.0f);
			//float c = Mathf.Min(Mathf.Abs(1-v.Dot(a)), 1.0f);
			float c = CounterAccelFactor.SampleBaked(1-v.Dot(a));
			//GD.Print("v: " + v + " a: " + a + " c: " + c);
			float avAngle = Mathf.Atan2((a.Y*v.X) - (a.X*v.Y), (a.Y*v.Y) + (a.X*v.X));
			Vector3 d = -(new Vector3(v.X, 0, v.Y).Normalized().Rotated(Vector3.Up, avAngle * DecelerationDirMod)) * deceleration * c * LateralCofactor;
			d = d * (Mathf.Pow(velocity.Length(), LateralCofactor2)*new Vector2(d.X, d.Z).Normalized().Dot(v));
			//d = d * Mathf.Pow(d.Length(), 0.5f);
			//d = d.Rotated(Vector3.Up, avAngle);
			
			velocity -= d;
			
			
			
			velocityXZ = new Vector3(velocity.X, 0, velocity.Z);
			
			Vector2 right = new Vector2(1, 0);
			Vector2 dXZ = new Vector2(d.X, d.Z);
			debugUI.v = velocity;
			debugUI.a = acceleration;
			debugUI.c = c;
			debugUI.avAngle = Mathf.RadToDeg(avAngle);
			VArm.SetRotation(MathF.Atan2(v.Y, v.X));
			DArm.SetRotation(MathF.Atan2(dXZ.Y, dXZ.X));
			DArm.SetScale(new Vector2(d.Length()/deceleration, DArm.GetScale().Y)*(debugUI.ShowBreakAsForce.ButtonPressed ? -1 : 1));
			AArm.SetRotation(MathF.Atan2(a.Y, a.X));
			MArm.SetRotation(MathF.Atan2(smoothedDirection.Z, smoothedDirection.X));
		}
		else
		{
			velocityXZ = new Vector3(velocity.X, 0, velocity.Z);
			// Decelerate
			
			velocity -= velocityXZ.Normalized() * deceleration;
		}
		
		// Handle Dash
		if (Input.IsActionJustPressed("move_dash") && DashTimer.TimeLeft == 0.0f)
		{
			var dashDirection = smoothedDirection;
			if (movementDirection.Length() == 0.0f)
			{
				dashDirection = new Vector3(0, 0, -1).Rotated(Vector3.Up, firstPersonCamera.Rotation.Y);
			}
			GD.Print("dashing in: " + dashDirection);
			
			var dashPower = DashVelocity;
		
			var dashVector = dashDirection * dashPower;
			velocity += dashVector;
			
			DashTimer.Start();
			
		}
		
		// Handle Jump
		if ((Input.IsActionPressed("move_jump") || Input.IsJoyButtonPressed(0, JoyButton.A)) && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
			
			var boostDirection = smoothedDirection;
			if (movementDirection.Length() == 0.0f)
			{
				boostDirection = Vector3.Zero;
			}
			velocity += boostDirection * JumpSpeedBoost * (Input.IsActionPressed("move_sprint") ? 1.0f : 0.0f);
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
