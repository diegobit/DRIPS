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
					Type ret = HandleMessage(msg);

					switch (ret)
					{
						case Type.Info:
							Console.WriteLine("RECEIVED INFO-MESSAGE (" + msg.Length + " Bytes)\n" + msg);
							break;
						case Type.FrequencyLeft:
						case Type.FrequencyFront:
						case Type.FrequencyRight:
							Console.WriteLine("IGNORED FREQUENCY-MESSAGE");
							break;
						case Type.None:
							Console.WriteLine("RECEIVED UNKNOWN OR CORRUPT MESSAGE (" + msg.Length + " Bytes)\n" + msg);
							break;
					}
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
        Type HandleMessage(string msg)
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
				}
			}

			return Type.None;
		}

        Type HandleInfoMessage(string msg)
		{
			if (msg.Length == 23)
			{
				RoadID roadID = (RoadID) msg[1];

				if (Enum.IsDefined(typeof(RoadID), roadID))
				{
					int orientation = Convert.ToInt32(msg.Substring(18, 3).Trim());

					string manufacturer = msg.Substring(2, 8).Trim();
					string model = msg.Substring(10, 8).Trim();
					ReqAction requestedAction = (ReqAction) msg[21];
					CurrAction currentAction = (CurrAction) msg[22];

					if (Enum.IsDefined(typeof(ReqAction), requestedAction) ||
                        Enum.IsDefined(typeof(CurrAction), currentAction))
					{
						monitor.UpdateRoad(roadID, orientation, manufacturer, model,
						                   requestedAction, currentAction);
						return Type.Info;
					}
				}
			}

			return Type.None;
		}

		/**
		 * To handle a frequency message is not this software's duty, so it
		 * simply returns the type of the message. If this method returns
		 * Type.FrequencyLeft it's not a guarantee of a well-formed message
		 */
        Type HandleFrequencyMessage(string msg)
		{
			if (msg.Length >= 130 && msg.Length <= 395)
			{
				return Type.FrequencyLeft;
			}

			return Type.None;
		}
	}
}