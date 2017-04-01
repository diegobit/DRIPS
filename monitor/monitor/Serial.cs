using System;
using System.IO.Ports;
using System.Threading;

namespace monitor
{
	public class Serial
	{
		MainWindow window;
		SerialPort port;
		Thread reader;
		volatile bool shouldTerminate = false;



		public Serial(MainWindow window, string portAddress, int baudRate)
		{
			this.window = window;

			port = new SerialPort(portAddress, baudRate);
		}



		/*
		 * Start the thread reading the data from the serial port,
		 * then it triggers and update of the UI
		 */
		public void startReading()
		{
			shouldTerminate = false;

			reader = new Thread(delegate ()
			{
				port.ReadTimeout = 1000;
				int i = 1;
				// Try to open the port every second
				while (!port.IsOpen && !shouldTerminate)
				{
					try
					{
						if (!openPort())
						{
							Thread.Sleep(1000 * i);
							i++;
						}
						else
							Console.WriteLine("--- PORT OPEN, READING... ---");
					}
					catch (ThreadInterruptedException) { }
				}

				// The port is open, read
				while (!shouldTerminate)
				{
					try
					{
						string msg = port.ReadLine();
						Console.WriteLine("--> RECEIVED MESSAGE. LENGTH: " + msg.Length + " B\n" + msg);

						// Ensure interface updates are executed on main loop
						Gtk.Application.Invoke(delegate
						{
							window.Update(msg);
						});
					}
					catch (TimeoutException) { }
				}

				Console.WriteLine("Thread terminating...");
				closePort();
			});

			reader.IsBackground = true;
			reader.Start();
		}

		/*
		 * Terminate the thread reading data from the serial port (and close the port)
		 */
		public void stopReading()
		{
			if (reader != null)
			{
				shouldTerminate = true;
				reader.Interrupt();
				reader.Join();
				reader = null;
				Console.WriteLine("Serial port closed.");
			}
			else
			{
				Console.WriteLine("Cannot run stopReading() before startReading()");
			}
		}



		private bool openPort()
		{
			if (!port.IsOpen)
			{
				try
				{
					port.Open();
				}
				catch (System.IO.IOException)
				{
					Console.WriteLine("Serial port unavailable");
				}
			}
			return port.IsOpen;
		}

		private void closePort()
		{
			port.Close();
		}

		private void decodeMessage(string msg)
		{
			//TODO
		}
	}
}
