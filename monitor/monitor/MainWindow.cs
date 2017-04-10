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
    /**
     * For each road I keep 4 things:
     * car image, label, left signal, right signal
     */
    Dictionary<RoadID, Tuple<Image, Label, Image, Image>> roads;



	public MainWindow() : base(WindowType.Toplevel)
	{
		Build();

		unknownImagePath = "monitor.resources.car" + resDiv + "Unknown" + resDiv + "Unknown" + imageExtension;

        roads = new Dictionary<RoadID, Tuple<Image, Label, Image, Image>>();

        container = new Layout(null, null);
		Add(container);

		crossroadImage = Image.LoadFromResource("monitor.resources.crossroad.png");
		container.Put(crossroadImage, 0, 0);

		// Listen for window resizing events
		SizeAllocated += delegate
		{
			if (!stopPropagate) /* FIXME: workaround for an endless number of calls to SizeAllocated
								(Moving a widget in OnResizeImages() triggers this same event) */
			{
				OnResizeImages();
				stopPropagate = true;
			}
			else
			{
				stopPropagate = false;
			}
		};

		// Create a textview for each car in the road
		var roadvalues = (RoadID[])Enum.GetValues(typeof(RoadID));
		foreach (RoadID road in roadvalues)
		{
			if (road != RoadID.None)
			{
				Label label = LoadLabel(road);
				Tuple<Image, Image> s = LoadSignalImages(road);

				roads.Add(road, Tuple.Create((Image)null, label, s.Item1, s.Item2));

				PlaceLabel(label, road);
				Application.Invoke(delegate /* FIXME: workaround: by executing it this way 
				the method is delayed long enough for the Hide() on the signals to be effective */
				{
					PlaceSignals(s.Item1, s.Item2, road);
				});

			}
		}
	}

	public void UpdateRoad(Road road)
	{
        roads.TryGetValue(road.Id, out Tuple<Image, Label, Image, Image> car);
        string expectedImagePath = "monitor.resources.car" + resDiv + road.Manufacturer + resDiv + road.Model + imageExtension;

		// Update text beside the car
		Application.Invoke(delegate
		{
            car.Item2.Text = MakeCarLabelText(road);
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
                roads[road.Id] = Tuple.Create(carImg, car.Item2, car.Item3, car.Item4);
                RemoveCar(prevPath);
				PlaceCar(carImg, road);
			}
		}
		else
		{
            // Image not present, I place it
			Image carImg = LoadCarImage(expectedImagePath, road.Id);
            roads[road.Id] = Tuple.Create(carImg, car.Item2, car.Item3, car.Item4);
			PlaceCar(carImg, road);
 		}

		// Update signals
		Image leftSignal = car.Item3;
		Image rightSignal = car.Item4;
		leftSignal.Hide(); // Done to synchronize the GIF playback of the two signals
		rightSignal.Hide();
		if (road.Priority == Priority.High)
		{
			leftSignal.Show();
			rightSignal.Show();
		}
		else if (road.RequestedAction == monitor.Action.Left)
		{
			leftSignal.Show();
			rightSignal.Hide();
		}
		else if (road.RequestedAction == monitor.Action.Right)
		{
			leftSignal.Hide();
			rightSignal.Show();
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

    Tuple<Image, Image> LoadSignalImages(RoadID road)
    {
        Image l = new Image();
        Image r = new Image();
		switch (road)
		{
			case (RoadID.Bottom):
				l.PixbufAnimation = new Gdk.PixbufAnimation(null, "monitor.resources.signal_left.gif");
				r.PixbufAnimation = new Gdk.PixbufAnimation(null, "monitor.resources.signal_right.gif");
				break;
			case (RoadID.Left):
				l.PixbufAnimation = new Gdk.PixbufAnimation(null, "monitor.resources.signal_up.gif");
				r.PixbufAnimation = new Gdk.PixbufAnimation(null, "monitor.resources.signal_down.gif");
				break;
			case (RoadID.Top):
				l.PixbufAnimation = new Gdk.PixbufAnimation(null, "monitor.resources.signal_right.gif");
				r.PixbufAnimation = new Gdk.PixbufAnimation(null, "monitor.resources.signal_left.gif");
				break;
			case (RoadID.Right):
				l.PixbufAnimation = new Gdk.PixbufAnimation(null, "monitor.resources.signal_down.gif");
				r.PixbufAnimation = new Gdk.PixbufAnimation(null, "monitor.resources.signal_up.gif");
				break;
		}
		return Tuple.Create(l, r);
	}

	Label LoadLabel(RoadID road)
	{
		Label label = new Label();
		label.SetSizeRequest(280, 120);
		label.ModifyBase(StateType.Normal, new Gdk.Color(230, 230, 230));
		label.ModifyFont(Pango.FontDescription.FromString("Arial 20"));
		label.Text = MakeCarLabelText();
		if (road == RoadID.Left || road == RoadID.Top)
			label.Justify = Justification.Right;
		return label;
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
		car.Pixbuf = car.Pixbuf.RotateSimple((Gdk.PixbufRotation)road.Orientation); //TODO: only allowed 90 degrees step

		Application.Invoke(delegate
		{
			container.Put(car, pos.Item1, pos.Item2);
			car.Show();
		});
	}

	void PlaceSignals(Image leftSignal, Image rightSignal, RoadID road)
    {
		Tuple<int, int, int, int> pos = ComputeSignalPositions(road, leftSignal);
		int xl = pos.Item1;
		int yl = pos.Item2;
		int xr = pos.Item3;
		int yr = pos.Item4;

		container.Put(leftSignal, xl, yl);
		container.Put(rightSignal, xr, yr);
		leftSignal.Hide();
		rightSignal.Hide();
    }

	void PlaceLabel(Label label, RoadID road)
	{
		Tuple<int, int> pos = ComputeLabelPosition(road, label);
		container.Put(label, pos.Item1, pos.Item2);
	}



    Tuple<int, int> ComputeCarPosition(RoadID road, Image car)
    {
		int crossW = crossroadImage.Allocation.Width;
		int crossH = crossroadImage.Allocation.Height;
		int carLong = car.Pixbuf.Height > car.Pixbuf.Width ? car.Pixbuf.Height : car.Pixbuf.Width;
		int carShort = car.Pixbuf.Height < car.Pixbuf.Width ? car.Pixbuf.Height : car.Pixbuf.Width;
		int stepToMiddleShortW = (rRadW - carShort) / 2;
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

	/**
	 * returns the positions of the signals of the car on `road`.
	 * Assumptions: image `signal` already rotated; the two signals have the same size
	 */
	Tuple<int, int, int, int> ComputeSignalPositions(RoadID road, Image signal)
    {
		int crossW = crossroadImage.Allocation.Width;
		int crossH = crossroadImage.Allocation.Height;
		int sW = signal.PixbufAnimation.Width;
		int sH = signal.PixbufAnimation.Height;

		int xl = 0;
		int yl = 0;
		int xr = 0;
		int yr = 0;
		switch (road)
		{
			case (RoadID.Bottom):
				xl = crossW / 2;
				yl = crossH / 2 + rRadH - sH;
				xr = xl + rRadW - sW;
				yr = yl;
				break;
			case (RoadID.Left):
				xl = crossW / 2 - rRadW;
				yl = crossH / 2;
				xr = xl;
				yr = yl + rRadH - sH;
				break;
			case (RoadID.Top):
				xl = crossW / 2 - sW;
				xr = xl - rRadW + sW;
				yl = crossH / 2 - rRadH;
				yr = yl;
				break;
			case (RoadID.Right):
				xl = crossW / 2 + rRadW - sW;
				xr = xl;
				yl = crossH / 2 - sH;
				yr = yl - rRadH + sH;
				break;
		}

		return Tuple.Create(xl, yl, xr, yr);
    }

	string MakeCarLabelText(Road road)
	{
        if (road.IsEmpty())
            return MakeCarLabelText();
        if (road.IsPartial())
            return "Unknown model" + '\n';
        if (road.IsComplete())
            return road.Manufacturer + " " + road.Model + "\n" +
                   "\n" +
                   "Requested action: " + road.RequestedAction + "\n" +
                   "Current action: " + road.CurrentAction + "\n" +
                   "Priority: " + road.Priority + "\n";

        // else
        Console.WriteLine("ERROR: road in wrong state:");
        Console.WriteLine(road);
        return MakeCarLabelText();
	}

    /**
     * Returns the text of an empty road label
     */
    string MakeCarLabelText()
    {
        return "Road empty" + '\n';
    }


    /**
     * Repositions car images and labels
     * Method called by the window when a SizeAllocated event occurs
     */
    void OnResizeImages()
        {
        foreach (RoadID road in roads.Keys)
        {
            Tuple<Image, Label, Image, Image> t = roads[road];
            Image car = t.Item1;
            Label label = t.Item2;
			Image leftSignal = t.Item3;
			Image rightSignal = t.Item4;

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

			if (leftSignal != null && rightSignal != null)
			{
				Tuple<int, int, int, int> pos = ComputeSignalPositions(road, leftSignal);
				container.Move(leftSignal, pos.Item1, pos.Item2);
				container.Move(rightSignal, pos.Item3, pos.Item4);
			}
        }
    }

	protected void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		Application.Quit();
		a.RetVal = true;
	}
}