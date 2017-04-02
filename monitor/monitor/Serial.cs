using System;
using System.IO.Ports;
using System.Threading;

namespace monitor
{
	public enum Type
	{
		Info = 'I',
		FrequencyLeft = 'L',
		FrequencyFront = 'F',
		FrequencyRight = 'R'
	}

	public enum RoadID
	{
		My = 'M',
		Left = 'L',
		Front = 'F',
		Right = 'R'
	}

	public enum Priority
	{
		Normal = 'N',
		High = 'Y'
	}

	public enum Action
	{
		Still = 'S',
		Left = 'L',
		Straight = 'A',
		Right = 'R'
	}



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

						if (!handleMessage(msg))
							Console.WriteLine("Received corrupted or unknown message");
						else 
							Console.WriteLine("Received valid message"); //TODO RM
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
		 * Terminate the thread reading data from the serial port (and close the port).
		 * Should NOT be called from withing the reader thread.
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



		/*
		 * Decode the message and update the UI
		 */
		private bool handleMessage(string msg)
		{
			Type msgType = (Type)Convert.ToInt32(msg.Substring(0, 1));
			switch (msgType)
			{
				case Type.Info:
					return handleInfoMessage(msg);
				case Type.FrequencyLeft:
				case Type.FrequencyFront:
				case Type.FrequencyRight:
					return handleFrequencyMessage(msg);
				default:
					return false;
			}
		}

		private bool handleInfoMessage(string msg)
		{
			if (msg.Length == 24)
			{
				RoadID roadID = (RoadID)Convert.ToInt32(msg.Substring(1, 1));
				string manufacturer = msg.Substring(2, 8).Trim();
				string model = msg.Substring(10, 8).Trim();
				int orientation = Convert.ToInt32(msg.Substring(18, 3).Trim());
				Priority priority = (Priority)Convert.ToInt32(msg.Substring(21, 1));
				Action requestedAction = (Action)Convert.ToInt32(msg.Substring(22, 1));
				Action currentAction = (Action)Convert.ToInt32(msg.Substring(23, 1));

				if (Enum.IsDefined(typeof(RoadID), roadID) ||
					Enum.IsDefined(typeof(Priority), priority) ||
					Enum.IsDefined(typeof(Action), priority) ||
					Enum.IsDefined(typeof(Action), priority))
				{
					window.Update(roadID, manufacturer, model, orientation, priority, requestedAction, currentAction);	
				}
			}

			return false;
		}

		private bool handleFrequencyMessage(string msg)
		{
			if (msg.Length >= 130 && msg.Length <= 395)
			{
				// TODO: start the python script?
				return true;
			}

			return false;
		}
	}
}