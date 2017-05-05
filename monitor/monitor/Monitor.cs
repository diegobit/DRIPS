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
		FrequencyRight = 'R'
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
		None = '0',
		Normal = 'N',
		High = 'Y'
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

		public Monitor(MainWindow window)
		{
			this.window = window;

			crossroad = new Dictionary<RoadID, Road>();

			s = new Serial(this, "/dev/tty.usbmodem1D121", 230400); //TODO: better port choice
		}

		public void StartSerialPortReading()
		{
			s.StartReading();
		}

		public void UpdateRoad(RoadID roadID, bool isEmpty, int orientation, string manufacturer, string model,
							   Priority priority, RequestedAction requestedAction, CurrentAction currentAction)
		{
            if (!crossroad.TryGetValue(roadID, out Road r))
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



		public static void Main()
		{
			Application.Init();
			MainWindow w = new MainWindow();
			Monitor m = new Monitor(w);
			w.ShowAll();
			m.StartSerialPortReading();
			Application.Run();
		}
	}
}
