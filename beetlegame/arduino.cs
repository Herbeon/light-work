using Godot;
using System;
using System.IO.Ports;

public partial class arduino : Node2D
{
	// Called when the node enters the scene tree for the first time.
	SerialPort serialPort;
	RichTextLabel text;

	float timer;
	int lalala = 0;
	public override void _Ready()
	{
		text = GetNode<RichTextLabel>("RichTextLabel");
		serialPort = new SerialPort();
		serialPort.PortName = "/dev/ttyACM0";
		serialPort.BaudRate = 9600;
		serialPort.Open();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (!serialPort.IsOpen)
		{
			return;
		}
		string serialMessage = serialPort.ReadLine();
		// gets heree
		if (serialMessage == "h"){
			lalala++;
			// cannot get here ._.
			text.Text = "HELLOBACK" + lalala;
		}
	}
}
