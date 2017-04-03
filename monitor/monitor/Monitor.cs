using System;
using Gtk;

namespace monitor
{
	public enum Type
	{
		None = 0,
		Info = 'I',
		FrequencyLeft = 'L',
		FrequencyFront = 'F',
		FrequencyRight = 'R'
	}

	public enum RoadID
	{
		None = 0,
		Bottom = 'M',
		Left = 'L',
		Top = 'F',
		Right = 'R'
	}

	public enum Priority
	{
		None = 0,
		Normal = 'N',
		High = 'Y'
	}

	public enum Action
	{
		None = 0,
		Still = 'S',
		Left = 'L',
		Straight = 'A',
		Right = 'R'
	}



	public class Monitor
	{
		public static void Main(string[] args)
		{
			Application.Init();
			Monitor m = new Monitor();
			MainWindow w = new MainWindow();

			Serial s = new Serial(w, "/dev/tty.usbmodem1D121", 230400); //TODO: better port choice
			s.startReading();
			w.ShowAll();
			Application.Run();
		}
	}
}
