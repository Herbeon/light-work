using Godot;
using System;

public partial class Player : CharacterBody2D
{
	public const float Speed = 500.0f;
	public const float JumpVelocity = -900.0f;
	private AnimatedSprite2D _animatedSprite;
	private AnimatedSprite2D _animatedwing1;
	private AnimatedSprite2D _animatedwing2;


	public override void _Ready()
	{
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_animatedwing1 = GetNode<AnimatedSprite2D>("AnimatedSprite2D2");
		_animatedwing2 = GetNode<AnimatedSprite2D>("AnimatedSprite2D3");

	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;
		// _animatedSprite.Play("default");

		// Add the gravity.
		if (IsOnFloor())
		{
			_animatedSprite.Play("sitting");
			_animatedwing1.Play("sitting");
			_animatedwing2.Play("sitting");
		}
		if (!IsOnFloor())
		{
			_animatedSprite.Play("default");
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("up"))
		{
			velocity.Y = JumpVelocity;
			_animatedwing1.SpeedScale = velocity.Y/800;
			_animatedwing2.SpeedScale = velocity.Y/800;

			_animatedwing1.Play("default");
			_animatedwing2.Play("default");
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
