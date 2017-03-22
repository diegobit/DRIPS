using System;
using Gtk;
using System.IO.Ports;

namespace monitor
{
	public class Monitor
	{
		public static void Main(string[] args)
		{
			Application.Init();
			Monitor m = new Monitor();
			MainWindow w = new MainWindow(m);

			Serial s = new Serial(w);
			w.ShowAll();

			Application.Run();
		}
	}

	class Serial
	{
		MainWindow w;
		//SerialPort port = new SerialPort("/dev/tty.usbmodem1D121", 9600, Parity.None, 19, StopBits.One);
		SerialPort sp;

		public Serial(MainWindow window)
		{
			w = window;

			// Create the serial port with basic settings
			sp = new SerialPort("/dev/tty.usbmodem1D111", 9600);
			//sp.ReadTimeout = 400;

			sp.Open();

			Console.Write(sp.ReadLine()); // removes the \n
			Console.Write(sp.ReadLine());
			Console.Write(sp.ReadLine());
			Console.Write(sp.ReadLine());

			//// Ensure interface updates are executed on main loop
			//Gtk.Application.Invoke(delegate
			//{
			//	w.UpdateActionText(data);
			//});
		}
	}
}
