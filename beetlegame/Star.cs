using Godot;
using System;
using System.ComponentModel;


public partial class Star : Area2D
{
	[Signal]
	public delegate void CollectedStarEventHandler(); 

	int score = 0;
	private AnimatedSprite2D _animatedStar;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_animatedStar = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_animatedStar.Play("default");
		// CollectedStar.connect(colley());

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
	public void _on_body_entered(Node2D body)
	{
		if(_animatedStar.Animation != "explod")
		{
			GD.Print("FIRST EXPLOD!!");
			score++;
			// this score is local to the star, need to make the thing
			
		}
		_animatedStar.Play("explod");

		EmitSignal(nameof(CollectedStar));	
	}

	// public void colley()
	// {
	// 	score++;
	// 	GD.Print("stars collected: " + score);
	// }
}
