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

		int negx = 0;

		int posx = 0;

		var whee = new Vector2
		{
			Y = 0
		};
		Vector2 velocity = Velocity;
		if (!serialPort.IsOpen)
		{
			return;
		}
		if(serialPort.BytesToRead > 0)
		{
			string serialMessage = serialPort.ReadLine();
			// LEFT
			// if(serialMessage.IndexOf("LIDLE") != -1)
			// {
				
			// }
			// if(serialMessage.IndexOf("LGENTLE") != -1 || serialMessage.IndexOf("LMODERATE") != -1 || serialMessage.IndexOf("LSTRONG") != -1)
			// {
			// 	velocity.Y = JumpVelocity;
			// 	_animatedwing1.Play("default");
			// }
			// idle, gentle, moderate, strong
			if (serialMessage.IndexOf("LEFT")!= -1)
			{
				// lefty
				if(serialMessage.IndexOf("IDLE") == -1)
				{
					whee.X += 1;
					// if(posx < 1)
					// {
					// 	posx += 1;
					// }
				}
				if(serialMessage.IndexOf("GENTLE") != -1)
				{
					// idle
					// left gentle

				}
				else if(serialMessage.IndexOf("MODERATE") != -1)
				{
					// left moderate
					velocity.Y = JumpVelocity;
					_animatedwing1.Play("default");
				}
				else if(serialMessage.IndexOf("STRONG") != -1)
				{
					velocity.Y = JumpVelocity;
					_animatedwing1.Play("default");
					// left strong
				}
			}
			// if (serialMessage == "h"){
			// 	velocity.Y = JumpVelocity;

			// 	GD.Print("textytextext");
			// }
			else if (serialMessage.IndexOf("RIGHT") != -1){
				// lefty
				if(serialMessage.IndexOf("IDLE") == -1)
					{
						// if(negx < 1)
						// {
						// 	negx += 1;
						// }
						whee.X -= 1;
					}
				if(serialMessage.IndexOf("GENTLE") != -1)
				{
					// idle
					// left gentle
					velocity.Y = JumpVelocity;
					_animatedwing2.Play("default");
				}
				else if(serialMessage.IndexOf("MODERATE") != -1)
				{
					// left moderate
					velocity.Y = JumpVelocity;
					_animatedwing2.Play("default");
				}
				else if(serialMessage.IndexOf("STRONG") != -1)
				{
					velocity.Y = JumpVelocity;
					_animatedwing2.Play("default");
					// left strong
				}
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
		GD.Print(whee);
		if (whee != Vector2.Zero)
		{
			velocity.X = whee.X * Speed;
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
