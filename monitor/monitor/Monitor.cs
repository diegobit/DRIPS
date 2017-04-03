using System;
using System.Collections.Generic;
using Gtk;

namespace monitor
{
	public enum Type
	{
		None = 0,
		Info = 'I',
		FrequencyLeft = 'L',
		FrequencyFront = 'F',
		FrequencyRight = 'R'
	}

	public enum RoadID
	{
		None = 0,
		Bottom = 'M',
		Left = 'L',
		Top = 'F',
		Right = 'R'
	}

	public enum Priority
	{
		None = 0,
		Normal = 'N',
		High = 'Y'
	}

	public enum Action
	{
		None = 0,
		Still = 'S',
		Left = 'L',
		Straight = 'A',
		Right = 'R'
	}



	public class Monitor
	{
		MainWindow window;
		Dictionary <RoadID, Road> crossroad;

		public Monitor(MainWindow window)
		{
			this.window = window;

			crossroad = new Dictionary<RoadID, Road>();
			//crossroad.Add(RoadID.Bottom, new Road(RoadID.Bottom));
			//crossroad.Add(RoadID.Left, new Road(RoadID.Left));
			//crossroad.Add(RoadID.Top, new Road(RoadID.Top));
			//crossroad.Add(RoadID.Right, new Road(RoadID.Right));

			Serial s = new Serial(this, "/dev/tty.usbmodem1D121", 230400); //TODO: better port choice
			s.startReading();
		}

		public void UpdateRoad(RoadID roadID, int orientation)
		{
			Road r;
			if (!crossroad.TryGetValue(roadID, out r))
			{
				r = new Road(roadID);
				crossroad.Add(roadID, r);
			}
			r.Orientation = orientation;

			window.UpdateRoad(r);
		}

		public void UpdateRoad(RoadID roadID, int orientation, string manufacturer, string model,
							   Priority priority, Action requestedAction, Action currentAction)
		{
			Road r;
			if (!crossroad.TryGetValue(roadID, out r))
			{
				r = new Road(roadID);
				crossroad.Add(roadID, r);
			}
			r.Orientation = orientation;
			r.Manufacturer = manufacturer;
			r.Model = model;
			r.Priority = priority;
			r.RequestedAction = requestedAction;
			r.CurrentAction = currentAction;

			window.UpdateRoad(r);
		}



		private void UpdateUI(Road road)
		{
			
		}



		public static void Main(string[] args)
		{
			Application.Init();
			MainWindow w = new MainWindow();
			Monitor m = new Monitor(w);
			w.ShowAll();
			Application.Run();
		}
	}
}
