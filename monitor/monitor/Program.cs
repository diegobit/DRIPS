using System;
using Gtk;
using System.IO.Ports;

namespace monitor
{
	public class Monitor
	{
		public void SaySomething()
		{
			Console.Write("Something... ");
		}

		public static void Main(string[] args)
		{
			Application.Init();
			Monitor m = new Monitor();
			MainWindow win = new MainWindow(m);
			win.Show();
			Application.Run();
		}
	}
}
