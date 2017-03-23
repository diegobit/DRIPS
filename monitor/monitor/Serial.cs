using System;
using System.IO.Ports;
using System.Threading;

namespace monitor
{
	public class Serial
	{
		MainWindow w;
		//SerialPort port = new SerialPort("/dev/tty.usbmodem1D121", 9600, Parity.None, 19, StopBits.One);
		string port = "";
		SerialPort sp;

		public Serial(MainWindow window, string port)
		{
			w = window;
			this.port = port;

			// Create the serial port with basic settings
			sp = new SerialPort(port, 345600); //TODO: better port choice
																 //sp.ReadTimeout = 400;
			sp.Open();

			//// Ensure interface updates are executed on main loop
			//Gtk.Application.Invoke(delegate
			//{
			//	w.UpdateActionText(data);
			//});
		}

		public void startListening()
		{
			Thread t = new Thread(delegate()
			{
				while (true)
				{
					string a = sp.ReadLine();
					Console.Write(a.Length + "-" + a + "\n");
				}
			});
			t.Start();
		}
	}
}
