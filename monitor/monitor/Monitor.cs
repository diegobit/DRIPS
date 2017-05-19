using System;
using System.Collections.Generic;
using Gtk;

namespace monitor
{
	public enum Type
	{
		None = '0',
		Info = 'I',
		FrequencyLeft = 'L',
		FrequencyFront = 'F',
		FrequencyRight = 'R',
		RawLeft = 'l',
		RawFront = 'f',
		RawRight = 'r'
	}

	public enum RoadID
	{
		None = '0',
		Bottom = 'M',
		Left = 'L',
		Top = 'A',
		Right = 'R'
	}

	public enum Priority
	{
		Normal = '0',
		High = '1'
	}

	public enum RequestedAction
	{
		None = '0',
		Straight = 'A',
		Left = 'L',
		Right = 'R'
	}

	public enum CurrentAction
	{
		None = '0',
		Straight = 'A',
		Left = 'L',
		Right = 'R',
		Still = 'S',
	}



	public class Monitor
	{
		MainWindow window;
		Serial s;
		Dictionary <RoadID, Road> crossroad;
		public string logPath { get; } = "/tmp/drips-data-monitor";

		public Monitor(MainWindow window, string serialPort)
		{
			this.window = window;

			crossroad = new Dictionary<RoadID, Road>();

			s = new Serial(this, serialPort, 230400); //TODO: better port choice
		}

		public void StartSerialPortReading()
		{
			s.StartReading();
		}

		public void UpdateRoad(RoadID roadID, bool isEmpty, int orientation, string manufacturer, string model,
							   Priority priority, RequestedAction requestedAction, CurrentAction currentAction)
		{
            Road r;
            if (!crossroad.TryGetValue(roadID, out r))
            {
                r = new Road(roadID);
                crossroad.Add(roadID, r);
            }

			if (isEmpty)
			{
				r.RemoveCar();
			}
			else
			{
				r.IsEmpty = isEmpty;
				r.Orientation = orientation;
				r.Manufacturer = manufacturer;
				r.Model = model;
				r.Priority = priority;
				r.RequestedAction = requestedAction;
				r.CurrentAction = currentAction;
			}

			window.UpdateRoad(r);
		}

		public void Clean()
		{
			s.Clean();
		}



		public static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.Write("ERROR: need serial port as argument");
			}

			string serialPort = args[0];

			Application.Init();

			MainWindow w = new MainWindow();
			Monitor m = new Monitor(w, serialPort);
			w.Monitor = m;

			w.ShowAll();
			m.StartSerialPortReading();

			Application.Run();
		}
	}
}
