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
		Top = 'F',
		Right = 'R'
	}

	public enum ReqAction
	{
		None = '0',
		Left = 'L',
		Straight = 'A',
		Right = 'R',
		Priority = 'P'
	}

		public enum CurrAction
	{
		None = '0',
		Still = 'S',
		Left = 'L',
		Straight = 'A',
		Right = 'R'
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

		public void UpdateRoad(RoadID roadID, int orientation, string manufacturer, string model,
							   ReqAction requestedAction, CurrAction currentAction)
		{
            if (!crossroad.TryGetValue(roadID, out Road r))
            {
                r = new Road(roadID);
                crossroad.Add(roadID, r);
            }
            r.Orientation = orientation;
			r.Manufacturer = manufacturer;
			r.Model = model;
			r.RequestedAction = requestedAction;
			r.CurrentAction = currentAction;

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
