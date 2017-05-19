using System;
using System.IO.Ports;
using System.Threading;
using System.IO;

namespace monitor
{
	public class Serial
	{
		Monitor monitor;
		SerialPort port;
		Thread reader;
		volatile bool shouldTerminate; // false
		StreamWriter sw;



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

			// Delete old log file
			File.Delete(monitor.logPath);
			sw = new StreamWriter(monitor.logPath, true);

			// Read
			while (!shouldTerminate)
			{
				try
				{
					string msg = port.ReadLine();

					// Write log for the spectrum viewer
					sw.WriteLine(msg);
					sw.Flush();

					//var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
					//Console.WriteLine(appDataDir);

					// Write to file
					//string path = @"/tmp";
					//File.Exists(path);

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
						case Type.RawLeft:
						case Type.RawFront:
						case Type.RawRight:
							Console.WriteLine("IGNORED RAW-MESSAGE");
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

			// Stop reading
			sw.Close();
			File.Delete(monitor.logPath);
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

		public void Clean()
		{
			if (sw != null)
			{
				sw.Close();
				File.Delete(monitor.logPath);
			}
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
					case Type.RawLeft:
					case Type.RawFront:
					case Type.RawRight:
						return HandleFrequencyMessage(msg); //TODO RAW add
				}
			}

			return Type.None;
		}

        Type HandleInfoMessage(string msg)
		{
			if (msg.Length == 25) // 25 because I don't count the '\n'
			{
				RoadID roadID = (RoadID) msg[1];

				if (Enum.IsDefined(typeof(RoadID), roadID))
				{
					if (msg[2] == '1' || msg[2] == '0')
					{
						bool isEmpty = msg[2] == '1';
						string manufacturer = msg.Substring(3, 8).Trim();
						string model = msg.Substring(11, 8).Trim();
						int orientation = Convert.ToInt32(msg.Substring(19, 3).Trim());
						Priority priority = (Priority)msg[22];
						RequestedAction requestedAction = (RequestedAction)msg[23];
						CurrentAction currentAction = (CurrentAction)msg[24];

						if (Enum.IsDefined(typeof(Priority), priority) ||
							Enum.IsDefined(typeof(RequestedAction), requestedAction) ||
							Enum.IsDefined(typeof(CurrentAction), currentAction))
						{
							monitor.UpdateRoad(roadID, isEmpty, orientation, manufacturer, model,
											   priority, requestedAction, currentAction);
							return Type.Info;
						}
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