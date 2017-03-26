using System;
using System.IO.Ports;
using System.Threading;

namespace monitor
{
	public class Serial
	{
		MainWindow w;
		string port = "";
		int baudRate;
		SerialPort sp;

		public Serial(MainWindow window, string port, int baudRate)
		{
			w = window;
			this.port = port;
			this.baudRate = baudRate;

			sp = new SerialPort(port, baudRate);
			sp.Open();
		}

		public void startListening()
		{
			Thread t = new Thread(delegate()
			{
				while (true)
				{
					string a = sp.ReadLine();
					Console.Write(a.Length + "-" + a + "\n");

					//// Ensure interface updates are executed on main loop
					//Gtk.Application.Invoke(delegate
					//{
					//	w.UpdateActionText(data);
					//});
				}
			});
			t.Start();
		}
	}
}
