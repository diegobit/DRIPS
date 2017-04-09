using System;
using System.Collections.Generic;
using Gtk;
using monitor;

public partial class MainWindow : Window
{
    int rRadW = 152; // The width of half of the square at the center of the road
    int rRadH = 150;

	string resDiv = "_";
	string imageExtension = ".png";
	string unknownImagePath;

    bool stopPropagate;

	Layout container;
	Image crossroadImage;
    Dictionary<RoadID, Tuple<Image, Label>> roads;



	public MainWindow() : base(WindowType.Toplevel)
	{
		Build();
        Resize(1000, 700);

        unknownImagePath = "monitor.resources.car" + resDiv + "Unknown" + resDiv + "Unknown" + imageExtension;

        roads = new Dictionary<RoadID, Tuple<Image, Label>>();
        container = new Layout(null, null);

		crossroadImage = Image.LoadFromResource("monitor.resources.crossroad.png");
        container.Put(crossroadImage, 0, 0);

		// Create a textview for each car in the road
		var roadvalues = (RoadID[])Enum.GetValues(typeof(RoadID));
        foreach (RoadID road in roadvalues)
		{
            Label l = new Label();
			l.SetSizeRequest(280, 120);
			l.ModifyBase(StateType.Normal, new Gdk.Color(230, 230, 230));
			l.ModifyFont(Pango.FontDescription.FromString("Arial 20"));
            l.Text = MakeCarLabelText("", "", Priority.None, monitor.Action.None, monitor.Action.None);
            if (road == RoadID.Left || road == RoadID.Top)
                l.Justify = Justification.Right;

            roads.Add(road, Tuple.Create((Image)null, l));

            Tuple<int, int> pos = ComputeLabelPosition(road, l);
            container.Put(l, pos.Item1, pos.Item2);
		}

		// Put the container in the window
		Add(container);

		// Listen for window resizing events
		SizeAllocated += delegate
		{
			if (!stopPropagate)
			{
				OnResizeImages();
				stopPropagate = true;
			}
		};
	}



	public void UpdateRoad(Road road)
	{
        roads.TryGetValue(road.Id, out Tuple<Image, Label> car);
        string expectedImagePath = "monitor.resources.car" + resDiv + road.Manufacturer + resDiv + road.Model + imageExtension;

		// Update text beside the car
		Application.Invoke(delegate
		{
            car.Item2.Text = MakeCarLabelText(road.Manufacturer, road.Model, road.Priority, road.RequestedAction, road.CurrentAction);
		});

        // Update car image
        if (car.Item1 != null)
		{
			// Image present, I check whether I have to update its image
			string prevPath = car.Item1.Name;
			if (prevPath != (road.Id + expectedImagePath))
			{
                // The car has changed. Load the new image and place it;
			    Image carImg = LoadCarImage(expectedImagePath, road.Id);
                roads[road.Id] = Tuple.Create(carImg, car.Item2);
                RemoveCar(prevPath);
				PlaceCar(carImg, road);
			}
		}
		else
		{
            // Image not present, I place it
			Image carImg = LoadCarImage(expectedImagePath, road.Id);
            roads[road.Id] = Tuple.Create(carImg, car.Item2);
			PlaceCar(carImg, road);
 		}
	}



    Image LoadCarImage(string expectedImagePath, RoadID id)
	{
		Image car;
		// Image not present, I have to create it
		try
		{
			car = Image.LoadFromResource(expectedImagePath);
			car.Name = id + expectedImagePath;
		}
		catch (ArgumentException)
		{
			// The car advertised an unknown manufacturer or model
			car = Image.LoadFromResource(unknownImagePath);
			car.Name = id + unknownImagePath;
		}
		return car;
	}

    void RemoveCar(string imageName)
	{
		foreach (Widget w in container.Children)
		{
			try
			{
				Image i = (Image) w;
				if (i.Name == imageName)
				{
					Application.Invoke(delegate
					{
						container.Remove(w);
					});
				}
			}
			catch (InvalidCastException) { }
		}
	}

    void PlaceCar(Image car, Road road)
	{
        Tuple<int, int> pos = ComputeCarPosition(road.Id, car);

		Application.Invoke(delegate
		{
            container.Put(car, pos.Item1, pos.Item2);
			container.ShowAll();
		});
	}



    Tuple<int, int> ComputeCarPosition(RoadID road, Image car)
    {
		int crossW = crossroadImage.Allocation.Width;
		int crossH = crossroadImage.Allocation.Height;
		int carLong = car.Pixbuf.Height;
		int carShort = car.Pixbuf.Width;
		//int stepToMiddleLongW = (rRadW - carLong) / 2;
		int stepToMiddleShortW = (rRadW - carShort) / 2;
		//int stepToMiddleLongH = (rRadH - carLong) / 2;
		int stepToMiddleShortH = (rRadH - carShort) / 2;

        int x = 0;
        int y = 0;
		switch (road)
		{
			case RoadID.Bottom:
                x = crossW / 2 + stepToMiddleShortW;
                y = crossH / 2 + rRadH;
				break;
			case RoadID.Left:
                x = crossW / 2 - rRadW - carLong;
                y = crossH / 2 + stepToMiddleShortH;
				break;
			case RoadID.Top:
                x = crossW / 2 - rRadW + stepToMiddleShortW;
                y = crossH / 2 - rRadW - carLong;
				break;
			case RoadID.Right:
                x = crossW / 2 + rRadW;
                y = crossH / 2 - rRadH + stepToMiddleShortH;
				break;
		}

        return Tuple.Create(x, y);
    }

	Tuple<int, int> ComputeLabelPosition(RoadID road, Label label)
	{
		int x = 0;
		int y = 0;
		switch (road)
		{
			case (RoadID.Bottom):
				x = crossroadImage.Allocation.Width / 2 + rRadW;
				y = crossroadImage.Allocation.Height / 2 + rRadH;
				break;
			case (RoadID.Left):
				x = crossroadImage.Allocation.Width / 2 - rRadW - label.Allocation.Width;
				y = crossroadImage.Allocation.Height / 2 + rRadH;
				break;
			case (RoadID.Top):
				x = crossroadImage.Allocation.Width / 2 - rRadW - label.Allocation.Width;
				y = crossroadImage.Allocation.Height / 2 - rRadH - label.Allocation.Height;
				break;
			case (RoadID.Right):
				x = crossroadImage.Allocation.Width / 2 + rRadW;
				y = crossroadImage.Allocation.Height / 2 - rRadH - label.Allocation.Height;
				break;
		}

		return Tuple.Create(x, y);
	}

	string MakeCarLabelText(string manufacturer, string model, Priority priority,
							monitor.Action requestedAction, monitor.Action currentAction)
	{
		return (manufacturer == "" && model == "")
				? "Road empty"
				: manufacturer + " " + model + "\n" +
				  "\n" +
				  "Requested action: " + requestedAction + "\n" +
				  "Current action: " + currentAction + "\n" +
				  "Priority: " + priority + "\n";
	}


    /**
     * Repositions car images and labels
     * Method called by the window when a SizeAllocated event occurs
     */
    void OnResizeImages()
        {
        foreach (RoadID road in roads.Keys)
        {
            Tuple<Image, Label> t = roads[road];
            Image car = t.Item1;
            Label label = t.Item2;

			if (car != null)
			{
				Tuple<int, int> pos = ComputeCarPosition(road, car);
				container.Move(car, pos.Item1, pos.Item2);
			}

			if (label != null)
			{
				Tuple<int, int> pos = ComputeLabelPosition(road, label);
				container.Move(label, pos.Item1, pos.Item2);
			}
        }
    }

	protected void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		Application.Quit();
		a.RetVal = true;
	}
}