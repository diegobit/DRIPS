using System;
using Gtk;

namespace monitor
{
	public class Monitor
	{
		public static void Main(string[] args)
		{
			Application.Init();
			Monitor m = new Monitor();
			MainWindow w = new MainWindow(m);

			Serial s = new Serial(w, "/dev/tty.usbmodem1D121", 230400); //TODO: better port choice
			s.startListening();
			w.ShowAll();

			Application.Run();
		}
	}
}
