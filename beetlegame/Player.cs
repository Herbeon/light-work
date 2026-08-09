using Godot;
using System;
using System.IO.Ports;
using System.Threading;

public partial class Player : CharacterBody2D
{
	SerialPort serialPort;

	public const float Speed = 500.0f;
	public const float JumpVelocity = -800.0f;
	private AnimatedSprite2D _animatedSprite;
	private AnimatedSprite2D _animatedwing1;
	private AnimatedSprite2D _animatedwing2;


	public override void _Ready()
	{
		serialPort = new SerialPort();
		serialPort.PortName = "/dev/ttyACM0";
		serialPort.BaudRate = 9600;
		serialPort.Open();
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_animatedwing1 = GetNode<AnimatedSprite2D>("AnimatedSprite2D2");
		_animatedwing2 = GetNode<AnimatedSprite2D>("AnimatedSprite2D3");
	}

	public override void _PhysicsProcess(double delta)
	{
		// int xhelp = 0;

		var whee = new Vector2();
		whee.Y = 0;
		var rhee = new Vector2();
		rhee.Y = 0;

		Vector2 velocity = Velocity;
		if (!serialPort.IsOpen)
		{
			return;
		}
		if(serialPort.BytesToRead > 0)
		{
			string serialMessage = serialPort.ReadLine();
			// LEFT
			if(serialMessage.IndexOf("LIDLE") != -1)
			{
				whee.X = 0;
				// whee.Y = 0;	
				velocity.Y = JumpVelocity;
				_animatedwing1.Play("default");
			}
			if(serialMessage.IndexOf("LGENTLE") != -1)
			{
				whee.X = -0.87f;
				// whee.Y = 0.5f;
				velocity.Y = JumpVelocity;
				_animatedwing1.Play("default");
			}
			if(serialMessage.IndexOf("LMODERATE") != -1)
			{
				whee.X = -0.5f;
				// whee.Y = 0.87f;
				velocity.Y = JumpVelocity;
				_animatedwing1.Play("default");
			}
			if(serialMessage.IndexOf("LSTRONG") != -1)
			{
				whee.X = 0;
				// whee.Y = 1;
				velocity.Y = JumpVelocity;
				_animatedwing1.Play("default");
			}
			// idle, gentle, moderate, strong
			// if (serialMessage == "h"){
			// 	velocity.Y = JumpVelocity;

			// 	GD.Print("textytextext");
			// }
			if(serialMessage.IndexOf("RIDLE") != -1)
			{
				whee.X = 0;
				whee.Y = 0;
		
			}
			if(serialMessage.IndexOf("RGENTLE") != -1)
			{
				whee.X = 0.87f;
				velocity.Y = JumpVelocity;
				_animatedwing1.Play("default");
			}
			if(serialMessage.IndexOf("RMODERATE") != -1)
			{
				whee.X = 0.5f;
				velocity.Y = JumpVelocity;
				_animatedwing1.Play("default");
			}
			if(serialMessage.IndexOf("RSTRONG") != -1)
			{
				whee.X = 0;
				velocity.Y = JumpVelocity;
				_animatedwing1.Play("default");
			}
		}
		// Add the gravity.
		if (!IsOnFloor())
		{
			_animatedSprite.Play("default");
			velocity += GetGravity() * (float)delta;
			_animatedwing1.Play("default");
			_animatedwing2.Play("default");
		}
		else
		{
			_animatedSprite.Play("sitting");
			_animatedwing1.Animation = "sitting";
			_animatedwing2.Animation = "sitting";
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("left"))
		{
			velocity.Y = JumpVelocity;
			_animatedwing1.Play("default");

			// _animatedwing1.SpeedScale = Math.Abs(velocity.Y/800);
			// _animatedwing2.SpeedScale = Math.Abs(velocity.Y/800);
		}
		if (Input.IsActionJustPressed("right"))
		{
			velocity.Y = JumpVelocity;
	
			_animatedwing2.Play("default");

		}
		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		// Vector2 direction = Input.GetVector("left", "right", "up", "down");
		// Vector2 direction = new Vector2(posx-negx,0);
		// GD.Print(direction);
		// Vector2 direction = new Vector2(xhelp,0);
		whee = whee.Normalized();
		rhee = rhee.Normalized();
		GD.Print(whee);
		GD.Print(rhee);
		Vector2 dire = whee + rhee;
		if (dire != Vector2.Zero)
		{
			velocity.X = dire.X * Speed;
			// GD.Print(direction.X);
			// if(direction.X > 0)
			// {
			// 	_animatedwing1.Play("default");
			// }
			// if(direction.X < 0)
			// {
			// 	_animatedwing2.Play("default");
			// }
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			// _animatedSprite.Stop();
			// _animatedwing1.Stop();
			// _animatedwing2.Stop();
		}
		Velocity = velocity;

		MoveAndSlide();
	}
}
