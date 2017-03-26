using System;
using System.IO.Ports;
using System.Threading;

namespace monitor
{
	public class Serial
	{
		MainWindow w;
		SerialPort sp;

		public Serial(MainWindow window, string port, int baudRate)
		{
			w = window;

			sp = new SerialPort(port, baudRate);
			sp.Open();
		}

		public void startListening()
		{
			Thread t = new Thread(delegate()
			{
				Console.WriteLine("--- START READING ---");
				while (true)
				{
					string msg = sp.ReadLine();
					Console.WriteLine("\nRECEIVED MESSAGE. LENGTH: " + msg.Length + " B\n" + msg);

					//// Ensure interface updates are executed on main loop
					//Gtk.Application.Invoke(delegate
					//{
					//	w.UpdateActionText(msg);
					//});
				}
			});
			t.Start();
		}
	}
}
