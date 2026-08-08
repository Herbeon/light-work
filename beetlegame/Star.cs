using Godot;
using System;
using System.ComponentModel;


public partial class Star : Area2D
{
	private AnimatedSprite2D _animatedStar;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_animatedStar = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_animatedStar.Play("default");

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
	public void _on_body_entered(Node2D body)
	{
		GD.Print("hello");
		_animatedStar.Play("explod");

	}
}
