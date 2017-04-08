using System;
using System.Collections.Generic;
using Gtk;
using monitor;

public partial class MainWindow : Gtk.Window
{
	// FIXED WINDOW PARAMETERS
    int rRadW = 152;
    int rRadH = 150;

	string resDiv = "_";
	string imageExtension = ".png";
	string unknownImagePath;

    bool stopPropagate;

	Layout container;
	Image crossroadImage;
    Dictionary<RoadID, Tuple<Image, Label>> roads;



	public MainWindow() : base(Gtk.WindowType.Toplevel)
	{
		Build();
        Resize(1000, 700);

        unknownImagePath = "monitor.resources.car" + resDiv + "Unknown" + resDiv + "Unknown" + imageExtension;

        container = new Layout(null, null);
        roads = new Dictionary<RoadID, Tuple<Image, Label>>();

        // Create Widgets to put into the Fixed container
		crossroadImage = Image.LoadFromResource("monitor.resources.crossroad.png");
        //crossroadImage.RedrawOnAllocate = true;
        SizeRequested += delegate
        {
            Console.WriteLine("Window sizeRequested " + Allocation.Width + "-" + Allocation.Height);
        };
        SizeAllocated += delegate
        {
            Console.WriteLine("Window sizeAllocated " + Allocation.Width + "-" + Allocation.Height);
            //crossroadImage.SetSizeRequest(container.Allocation.Width, container.Allocation.Height);
            if (!stopPropagate)
            {
                ResizeAll(); //FIXME 
                stopPropagate = true;
            }
      };

        container.Put(crossroadImage, 0, 0);

		// Create a textview for each car in the road
		var roadvalues = (RoadID[])Enum.GetValues(typeof(RoadID));
        foreach (RoadID road in roadvalues)
		{
            Label l = new Label();
			l.SetSizeRequest(280, 120);
			l.ModifyBase(StateType.Normal, new Gdk.Color(230, 230, 230));
			l.ModifyFont(Pango.FontDescription.FromString("Arial 20"));
            l.Text = MakeCarText("", "", Priority.None, monitor.Action.None, monitor.Action.None);
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
	}



	public void UpdateRoad(Road road)
	{
        roads.TryGetValue(road.Id, out Tuple<Image, Label> car);
        string expectedImagePath = "monitor.resources.car" + resDiv + road.Manufacturer + resDiv + road.Model + imageExtension;

		// Update text beside the car
		Gtk.Application.Invoke(delegate
		{
            car.Item2.Text = MakeCarText(road.Manufacturer, road.Model, road.Priority, road.RequestedAction, road.CurrentAction);
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
			Image carImg = LoadCarImage(expectedImagePath, road.Id);
			//roads.Add(road.Id, car);
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
		catch (System.ArgumentException)
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
					Gtk.Application.Invoke(delegate
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

		Gtk.Application.Invoke(delegate
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
            ResizeCar(t.Item1, road);
            ResizeLabel(t.Item2, road);
        }
    }

    void ResizeCar(Image car, RoadID road)
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

            Gtk.Application.Invoke(delegate
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
                //container.ShowAll();
            });
        }
    }

    void ResizeLabel(Label label, RoadID road)
    {
        if (label != null)
        {
            Gtk.Application.Invoke(delegate
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

    string MakeCarText(string manufacturer, string model, Priority priority, monitor.Action requestedAction, monitor.Action currentAction)
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