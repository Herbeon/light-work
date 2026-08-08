using Godot;
using System;

public partial class Player : CharacterBody2D
{
	public const float Speed = 500.0f;
	public const float JumpVelocity = -800.0f;
	private AnimatedSprite2D _animatedSprite;
	private AnimatedSprite2D _animatedwing1;
	private AnimatedSprite2D _animatedwing2;


	public override void _Ready()
	{
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_animatedwing1 = GetNode<AnimatedSprite2D>("AnimatedSprite2D2");
		_animatedwing2 = GetNode<AnimatedSprite2D>("AnimatedSprite2D3");
		_animatedwing1.Play("sitting");
		_animatedwing2.Play("sitting");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;
		if (IsOnFloor())
		{
			_animatedSprite.Play("sitting");
			_animatedwing1.Play("sitting");
			_animatedwing2.Play("sitting");
		}
		// Add the gravity.
		if (!IsOnFloor())
		{
			_animatedSprite.Play("default");
			velocity += GetGravity() * (float)delta;
			if (_animatedwing1.Animation != "default")
			{
				_animatedwing1.Play("default");
			}
			if (_animatedwing2.Animation != "default")
			{
				_animatedwing2.Play("default");
			}
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("left"))
		{
			velocity.Y = JumpVelocity;
			if (_animatedwing1.Animation != "default")
			{
				_animatedwing1.Play("default");
			}

			// _animatedwing1.SpeedScale = Math.Abs(velocity.Y/800);
			// _animatedwing2.SpeedScale = Math.Abs(velocity.Y/800);
		}
		if (Input.IsActionJustPressed("right"))
		{
			velocity.Y = JumpVelocity;
			GD.Print(_animatedwing2.Animation);
			if (_animatedwing2.Animation != "default")
			{
				_animatedwing2.Play("default");
			}			// _animatedwing1.SpeedScale = Math.Abs(velocity.Y/800);
			// _animatedwing2.SpeedScale = Math.Abs(velocity.Y/800);
		}
		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 direction = Input.GetVector("left", "right", "up", "down");
		if (direction != Vector2.Zero)
		{
			velocity.X = direction.X * Speed;
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
			_animatedwing1.Stop();
			_animatedwing2.Stop();
		}

		Velocity = velocity;

		MoveAndSlide();
	}
}
