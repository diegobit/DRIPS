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
            switch(road)
            {
                case (RoadID.Bottom):
                    container.Put(l, crossroadImage.Allocation.Width / 2 + rRadW,
                                     crossroadImage.HeightRequest / 2 + rRadH);
                    break;
				case (RoadID.Left):
                    container.Put(l, crossroadImage.WidthRequest / 2 - rRadW - l.SizeRequest().Width,
                                     crossroadImage.HeightRequest / 2 + rRadH);
					break;
				case (RoadID.Top):
					container.Put(l, crossroadImage.WidthRequest / 2 - rRadW - l.SizeRequest().Width,
                                     crossroadImage.HeightRequest / 2 - rRadH - l.SizeRequest().Height);
					break;
				case (RoadID.Right):
					container.Put(l, crossroadImage.WidthRequest / 2 + rRadW,
                                     crossroadImage.HeightRequest / 2 - rRadH - l.SizeRequest().Height);
					break;
            }
		}

		// Put the container in the window
		Add(container);

		// Listen for window resizing events
		SizeAllocated += delegate
		{
			if (!stopPropagate)
			{
				ResizeAll();
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
        int crossW = crossroadImage.Allocation.Width;
        int crossH = crossroadImage.Allocation.Height;
        int carLong = car.Pixbuf.Height;
        int carShort = car.Pixbuf.Width;
        int stepToMiddleLongW = (rRadW - carLong) / 2;
        int stepToMiddleShortW = (rRadW - carShort) / 2;
        int stepToMiddleLongH = (rRadH - carLong) / 2;
        int stepToMiddleShortH = (rRadH - carShort) / 2;

		Application.Invoke(delegate
		{
			switch (road.Id)
			{
                case RoadID.Bottom:
                    container.Put(car, crossW / 2 + stepToMiddleShortW, crossH / 2 + rRadH);
                    break;
				case RoadID.Left:
					car.Pixbuf = car.Pixbuf.RotateSimple(Gdk.PixbufRotation.Clockwise);
                    container.Put(car, crossW / 2 - rRadW - carLong, crossH / 2 + stepToMiddleShortH);
					break;
				case RoadID.Top:
					car.Pixbuf = car.Pixbuf.RotateSimple(Gdk.PixbufRotation.Upsidedown);
                    container.Put(car, crossW / 2 - rRadW + stepToMiddleShortW, crossH / 2 - rRadW - carLong);
					break;
				case RoadID.Right:
					car.Pixbuf = car.Pixbuf.RotateSimple(Gdk.PixbufRotation.Counterclockwise);
                    container.Put(car, crossW / 2 + rRadW, crossH / 2 - rRadH + stepToMiddleShortH);
					break;
			}
			container.ShowAll();
		});
	}

    void ResizeAll()
        {
        foreach (RoadID road in roads.Keys)
        {
            Tuple<Image, Label> t = roads[road];
            ResizeCarImage(t.Item1, road);
            ResizeCarLabel(t.Item2, road);
        }
    }

    void ResizeCarImage(Image car, RoadID road)
    {
        if (car != null)
        {
            int crossW = crossroadImage.Allocation.Width;
            int crossH = crossroadImage.Allocation.Height;
			int carLong = car.Pixbuf.Width;
			int carShort = car.Pixbuf.Height;
			int stepToMiddleLongW = (rRadW - carLong) / 2;
			int stepToMiddleShortW = (rRadW - carShort) / 2;
			int stepToMiddleLongH = (rRadH - carLong) / 2;
			int stepToMiddleShortH = (rRadH - carShort) / 2;

            Application.Invoke(delegate
            {
                switch (road)
                {
                    case RoadID.Bottom:
                        container.Move(car, crossW / 2 + stepToMiddleShortW, crossH / 2 + rRadH);
                        break;
                    case RoadID.Left:
                        container.Move(car, crossW / 2 - rRadW - carLong, crossH / 2 + stepToMiddleShortH);
                        break;
                    case RoadID.Top:
                        container.Move(car, crossW / 2 - rRadW + stepToMiddleShortW, crossH / 2 - rRadW - carLong);
                        break;
                    case RoadID.Right:
                        container.Move(car, crossW / 2 + rRadW, crossH / 2 - rRadH + stepToMiddleShortH);
                        break;
                }
                container.ShowAll();
            });
        }
    }

    void ResizeCarLabel(Label label, RoadID road)
    {
        if (label != null)
        {
            Application.Invoke(delegate
            {
                switch (road)
                {
                    case (RoadID.Bottom):
                        container.Move(label, crossroadImage.Allocation.Width / 2 + rRadW,
                                       crossroadImage.Allocation.Height / 2 + rRadH);
                        break;
                    case (RoadID.Left):
                        container.Move(label, crossroadImage.Allocation.Width / 2 - rRadW - label.Allocation.Width,
                                       crossroadImage.Allocation.Height / 2 + rRadH);
                        break;
                    case (RoadID.Top):
                        container.Move(label, crossroadImage.Allocation.Width / 2 - rRadW - label.Allocation.Width,
                                       crossroadImage.Allocation.Height / 2 - rRadH - label.Allocation.Height);
                        break;
                    case (RoadID.Right):
                        container.Move(label, crossroadImage.Allocation.Width / 2 + rRadW,
                                       crossroadImage.Allocation.Height / 2 - rRadH - label.Allocation.Height);
                        break;
                }
            });
        }
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



	protected void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		Application.Quit();
		a.RetVal = true;
	}
}