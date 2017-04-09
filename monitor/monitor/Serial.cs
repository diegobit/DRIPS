using System;
using System.IO.Ports;
using System.Threading;

namespace monitor
{
	public class Serial
	{
		Monitor monitor;
		SerialPort port;
		Thread reader;
		volatile bool shouldTerminate; // false



		public Serial(Monitor monitor, string portAddress, int baudRate)
		{
			this.monitor = monitor;

			port = new SerialPort(portAddress, baudRate);
		}



		/*
		 * Start the thread reading the data from the serial port,
		 * then it triggers and update of the UI
		 */
		public void StartReading()
		{
			shouldTerminate = false;

            reader = new Thread(delegate ()
            {
                TryOpenPortUntilDone();
                ReadMessages(); // Executed until asked for termination

                Console.WriteLine("Thread terminating...");
                ClosePort();
            })
            {
                IsBackground = true
            };
            reader.Start();
		}

		/*
		 * Terminate the thread reading data from the serial port (and close the port).
		 * Should NOT be called from withing the reader thread.
		 */
        public void StopReading()
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



        void TryOpenPortUntilDone()
		{
			int i = 1;
			while (!port.IsOpen && !shouldTerminate)
			{
				try
				{
					if (!OpenPort())
					{
						Console.WriteLine("Serial port unavailable. Retrying in " + i + " seconds...");
						Thread.Sleep(1000 * i);
						i++;
					}
					else
						Console.WriteLine("--- PORT OPEN, READING STARTED ---");
				}
				catch (ThreadInterruptedException) { }
			}
		}

        void ReadMessages()
		{
			port.ReadTimeout = 1000;

			while (!shouldTerminate)
			{
				try
				{
					string msg = port.ReadLine();
					Console.WriteLine("[ RECEIVED MESSAGE. LENGTH: " + msg.Length + " B\n" + msg);

					if (!HandleMessage(msg))
						Console.WriteLine("UNKNOWN OR CORRUPT MESSAGE ]");
					else
						Console.WriteLine("MESSAGE HANDLED ]");
				}
				catch (TimeoutException) { }
				catch (System.IO.IOException)
				{
					ClosePort();
					TryOpenPortUntilDone();
				}
			}
		}

        bool OpenPort()
		{
			if (!port.IsOpen)
			{
				try
				{
					port.Open();
				}
				catch (System.IO.IOException) { }
			}
			return port.IsOpen;
		}

        void ClosePort()
		{
			port.Close();
		}



		/*
		 * Decode the message and update the monitor
		 */
        bool HandleMessage(string msg)
		{
			if (msg.Length > 0)
			{
				Type msgType = (Type)msg[0];
				switch (msgType)
				{
					case Type.Info:
						return HandleInfoMessage(msg);
					case Type.FrequencyLeft:
					case Type.FrequencyFront:
					case Type.FrequencyRight:
						return HandleFrequencyMessage(msg);
					default:
						break;
				}
			}

			return false;
		}

        bool HandleInfoMessage(string msg)
		{
			if (msg.Length == 24)
			{
				RoadID roadID = (RoadID) msg[1];

				if (Enum.IsDefined(typeof(RoadID), roadID))
				{
					int orientation = Convert.ToInt32(msg.Substring(18, 3).Trim());

					string manufacturer = msg.Substring(2, 8).Trim();
					string model = msg.Substring(10, 8).Trim();
					Priority priority = (Priority) msg[21];
					Action requestedAction = (Action) msg[22];
					Action currentAction = (Action) msg[23];

					if (Enum.IsDefined(typeof(Priority), priority) ||
                        Enum.IsDefined(typeof(Action), requestedAction) ||
                        Enum.IsDefined(typeof(Action), currentAction))
					{
						monitor.UpdateRoad(roadID, orientation, manufacturer, model,
						                   priority, requestedAction, currentAction);
						return true;
					}
				}
			}

			return false;
		}

        bool HandleFrequencyMessage(string msg)
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