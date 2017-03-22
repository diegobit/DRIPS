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
		// Create the serial port with basic settings
		SerialPort port = new SerialPort("COM1", 9600, Parity.None, 8, StopBits.One);

		public Serial(MainWindow window)
		{
			w = window;

			// Attach a method to be called when there is data waiting in the port's buffer
			port.DataReceived += new SerialDataReceivedEventHandler(dataReceivedHandler);

			// Begin communications
			//port.Open(); //FIXME: exception

			// Enter an application loop to keep this thread alive
			Application.Run(); //TODO ????
		}

		void dataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
		{
			// Show all the incoming data in the port's buffer
			string data = port.ReadExisting();

			Console.WriteLine(data);

			// Ensure interface updates are executed on main loop
			Gtk.Application.Invoke(delegate
			{
				w.UpdateActionText(data);
			});
		}
	}
}
