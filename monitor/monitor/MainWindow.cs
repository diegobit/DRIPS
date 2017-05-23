using System;
using System.Collections.Generic;
using Gtk;
using monitor;

public partial class MainWindow : Window
{
    // Crossroad parameters
    const int cFullW = 1366;
    const int cFullH = 768;
    const int cFullRadW = 152; // The width of half of the square at the center of the road
    const int cFullRadH = 150;
    int cRadW;
    int cRadH;

    // Label parameters
    const int labelFullW = 300;
    const int labelFullH = 140;
    int labelW;
    int labelH;
    const string fontFamily = "Arial";
    const int fontFullSize = 20;
    int fontSize;

    // Paths
    const string resDiv = "_";
    const string imageExtension = ".png";
    string unknownImagePath;

    RLayout container;
    Image crossroadImage;
    Gdk.Pixbuf crossroadPixbuf;
    /**
     * For each road I keep 4 things:
     * car image, label, left signal, right signal
     */
    Dictionary<RoadID, Tuple<Image, Label, Image, Image, Image>> roads;

    public Monitor Monitor { get; set; }



    public MainWindow() : base(WindowType.Toplevel)
    {
        Build();

        cRadW = cFullRadW;
        cRadH = cFullRadH;
        labelW = labelFullW;
        labelH = labelFullH;
        fontSize = fontFullSize;

        unknownImagePath = "monitor.resources.car" + resDiv + "Unknown" + resDiv + "Unknown" + imageExtension;

        roads = new Dictionary<RoadID, Tuple<Image, Label, Image, Image, Image>>();

        container = new RLayout(this, null, null);
        Add(container);

        crossroadImage = Image.LoadFromResource("monitor.resources.crossroad.png");
        crossroadPixbuf = crossroadImage.Pixbuf;
        container.Put(crossroadImage, 0, 0);

        // Create a textview for each car in the road
        var roadvalues = (RoadID[])Enum.GetValues(typeof(RoadID));
        foreach (RoadID road in roadvalues)
        {
            if (road != RoadID.None)
            {
                Label label = LoadLabel(road);
                Tuple<Image, Image> s = LoadSignalImages(road);
                Image aliveMarker = LoadAliveMarker(road);

                roads.Add(road, Tuple.Create((Image)null, label, s.Item1, s.Item2, aliveMarker));

                PlaceLabel(label, road);
                Application.Invoke(delegate /* FIXME: workaround: by executing it this way 
                                                      the method is delayed long enough for the Hide() on the signals to be effective */
                {
					PlaceAliveMarker(aliveMarker, road, label);
                    PlaceSignals(s.Item1, s.Item2, road);
                });

            }
        }
    }

    public void UpdateRoad(Road road)
    {
        Tuple<Image, Label, Image, Image, Image> car;
        roads.TryGetValue(road.Id, out car);
        string expectedManufacturer = road.Manufacturer == "" ? "Unknown" : road.Manufacturer;
        string expectedModel = road.Model == "" ? "Unknown" : road.Model;
        string hasPrecedenceString = road.CurrentAction == CurrentAction.Still ? "" : resDiv + "prec";
        string expectedImagePath = "monitor.resources.car" + resDiv + expectedManufacturer + resDiv + expectedModel + hasPrecedenceString + imageExtension;

        if (road.IsEmpty)
        {
            // Empty road, reset everything
            Application.Invoke(delegate
            {
                if (car.Item1 != null)
                {
                    container.Remove(car.Item1);
                }
                car.Item2.Text = MakeCarLabelText();
                car.Item3.Hide();
                car.Item4.Hide();
                car.Item5.Hide();
            });
            roads[road.Id] = Tuple.Create((Image)null, car.Item2, car.Item3, car.Item4, car.Item5);
        }
        else
        {
            // There's a car on that road, update the view
            // 1. Update car image
            if (car.Item1 != null)
            {
                // There was a car image before, I check whether I have to update it
                string prevPath = car.Item1.Name;
                if (prevPath != (road.Id + expectedImagePath))
                {
                    // The car has changed. Remove the old image from the view, load the new image and place it;
                    Application.Invoke(delegate
                    {
                        container.Remove(car.Item1);
                    });
                    Image carImg = LoadCarImage(expectedImagePath, road.Id);
                    roads[road.Id] = Tuple.Create(carImg, car.Item2, car.Item3, car.Item4, car.Item5);
                    PlaceCar(carImg, road);
                }
            }
            else
            {
                // Image not present, I place it
                Image carImg = LoadCarImage(expectedImagePath, road.Id);
                roads[road.Id] = Tuple.Create(carImg, car.Item2, car.Item3, car.Item4, car.Item5);
                PlaceCar(carImg, road);
            }

            // 2. Update text beside the car
            Application.Invoke(delegate
            {
                car.Item2.Text = MakeCarLabelText(road);

                // 3. Update signals
                Image leftSignal = car.Item3;
                Image rightSignal = car.Item4;
                if (road.Priority == Priority.High)
                {
                    if (!leftSignal.Visible || !rightSignal.Visible)
                    {
						// One of the two was hidden, so it just passed into Priority.High
						leftSignal.Hide(); // Done to synchronize the GIF playback of the two signals
						rightSignal.Hide();
                    }

                    leftSignal.Show();
                    rightSignal.Show();
                }
                else if (road.RequestedAction == RequestedAction.Left)
                {
                    leftSignal.Show();
                    rightSignal.Hide();
                }
                else if (road.RequestedAction == RequestedAction.Right)
                {
                    leftSignal.Hide();
                    rightSignal.Show();
                }
                else
                {
                    leftSignal.Hide();
                    rightSignal.Hide();
                }
            });

            // 4. refresh aliveMarker
            Image aliveMarker = car.Item5;
            car.Item5.Hide();
            car.Item5.Show();
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

    Image LoadAliveMarker(RoadID road)
    {
        Image s = new Image();
        s.PixbufAnimation = new Gdk.PixbufAnimation(null, "monitor.resources.alivemarker.gif");
        return s;
    }

    Label LoadLabel(RoadID road)
    {
        Label label = new Label();
        label.SetSizeRequest(labelW, labelH);
        label.ModifyBase(StateType.Normal, new Gdk.Color(230, 230, 230));
        label.ModifyFont(Pango.FontDescription.FromString(fontFamily + " " + fontSize));
        label.Text = MakeCarLabelText();
        if (road == RoadID.Left || road == RoadID.Top)
            label.Justify = Justification.Right;
        return label;
    }

    void PlaceCar(Image car, Road road)
    {
        Tuple<int, int> pos = ComputeCarPosition(road.Id, car, Allocation);
        int mod = road.Orientation % 90;
        int rotation = road.Orientation - mod + (mod > 45 ? 90 : 0); // round to nearest 90 degree step
        car.Pixbuf = car.Pixbuf.RotateSimple((Gdk.PixbufRotation)road.Orientation); //TODO: allow more than 90 degrees steps

        Application.Invoke(delegate
        {
            container.Put(car, pos.Item1, pos.Item2);
            car.Show();
        });
    }

    void PlaceSignals(Image leftSignal, Image rightSignal, RoadID road)
    {
        Tuple<int, int, int, int> pos = ComputeSignalPositions(road, leftSignal, Allocation);
        int xl = pos.Item1;
        int yl = pos.Item2;
        int xr = pos.Item3;
        int yr = pos.Item4;

        container.Put(leftSignal, xl, yl);
        container.Put(rightSignal, xr, yr);
        leftSignal.Hide();
        rightSignal.Hide();
    }

	void PlaceAliveMarker(Image aliveMarker, RoadID road, Label label)
	{
		Tuple<int, int> pos = ComputeAliveMarkerPosition(road, aliveMarker, label);
		int x = pos.Item1;
		int y = pos.Item2;

		container.Put(aliveMarker, x, y);
        aliveMarker.Hide();
	}

    void PlaceLabel(Label label, RoadID road)
    {
        Tuple<int, int> pos = ComputeLabelPosition(road, Allocation);
        container.Put(label, pos.Item1, pos.Item2);
    }



    Tuple<int, int> ComputeCarPosition(RoadID road, Image car, Gdk.Rectangle allocation)
    {
        int crossW = allocation.Width;
        int crossH = allocation.Height;
        int carLong = car.Pixbuf.Height > car.Pixbuf.Width ? car.Pixbuf.Height : car.Pixbuf.Width;
        int carShort = car.Pixbuf.Height < car.Pixbuf.Width ? car.Pixbuf.Height : car.Pixbuf.Width;
        int stepToMiddleShortW = (cRadW - carShort) / 2;
        int stepToMiddleShortH = (cRadH - carShort) / 2;

        int x = 0;
        int y = 0;
        switch (road)
        {
            case RoadID.Bottom:
                x = crossW / 2 + stepToMiddleShortW;
                y = crossH / 2 + cRadH;
                break;
            case RoadID.Left:
                x = crossW / 2 - cRadW - carLong;
                y = crossH / 2 + stepToMiddleShortH;
                break;
            case RoadID.Top:
                x = crossW / 2 - cRadW + stepToMiddleShortW;
                y = crossH / 2 - cRadW - carLong;
                break;
            case RoadID.Right:
                x = crossW / 2 + cRadW;
                y = crossH / 2 - cRadH + stepToMiddleShortH;
                break;
        }

        return Tuple.Create(x, y);
    }

    Tuple<int, int> ComputeLabelPosition(RoadID road, Gdk.Rectangle allocation)
    {
        int x = 0;
        int y = 0;
        switch (road)
        {
            case (RoadID.Bottom):
                x = allocation.Width / 2 + cRadW;
                y = allocation.Height / 2 + cRadH;
                break;
            case (RoadID.Left):
                x = allocation.Width / 2 - cRadW - labelW;
                y = allocation.Height / 2 + cRadH;
                break;
            case (RoadID.Top):
                x = allocation.Width / 2 - cRadW - labelW;
                y = allocation.Height / 2 - cRadH - labelH;
                break;
            case (RoadID.Right):
                x = allocation.Width / 2 + cRadW;
                y = allocation.Height / 2 - cRadH - labelH;
                break;
        }

        return Tuple.Create(x, y);
    }

    /**
     * returns the positions of the signals of the car on `road`.
     * Assumptions: image `signal` already rotated; the two signals have the same size
     */
    Tuple<int, int, int, int> ComputeSignalPositions(RoadID road, Image signal, Gdk.Rectangle allocation)
    {
        int sW = signal.PixbufAnimation.Width;
        int sH = signal.PixbufAnimation.Height;

        int xl = 0;
        int yl = 0;
        int xr = 0;
        int yr = 0;
        switch (road)
        {
            case (RoadID.Bottom):
                xl = allocation.Width / 2;
                yl = allocation.Height / 2 + cRadH - sH;
                xr = xl + cRadW - sW;
                yr = yl;
                break;
            case (RoadID.Left):
                xl = allocation.Width / 2 - cRadW;
                yl = allocation.Height / 2;
                xr = xl;
                yr = yl + cRadH - sH;
                break;
            case (RoadID.Top):
                xl = allocation.Width / 2 - sW;
                xr = xl - cRadW + sW;
                yl = allocation.Height / 2 - cRadH;
                yr = yl;
                break;
            case (RoadID.Right):
                xl = allocation.Width / 2 + cRadW - sW;
                xr = xl;
                yl = allocation.Height / 2 - sH;
                yr = yl - cRadH + sH;
                break;
        }

        return Tuple.Create(xl, yl, xr, yr);
    }

	Tuple<int, int> ComputeAliveMarkerPosition(RoadID road, Image aliveMarker, Label label)
	{
        int w = aliveMarker.PixbufAnimation.Width;
        int h = aliveMarker.PixbufAnimation.Height;

		int x = 0;
		int y = 0;

        switch (road)
		{
			case (RoadID.Bottom):
                x = label.Allocation.Right;
                y = label.Allocation.Top;
				break;
			case (RoadID.Left):
                x = label.Allocation.Left - aliveMarker.PixbufAnimation.Width;
                y = label.Allocation.Top;
				break;
			case (RoadID.Top):
                x = label.Allocation.Left - aliveMarker.PixbufAnimation.Width;
                y = label.Allocation.Bottom - aliveMarker.PixbufAnimation.Height;
				break;
			case (RoadID.Right):
                x = label.Allocation.Right;
                y = label.Allocation.Bottom - aliveMarker.PixbufAnimation.Height;
				break;
		}

		return Tuple.Create(x, y);
	}

    string MakeCarLabelText(Road road)
    {
        if (road.IsEmpty)
            return MakeCarLabelText();
        if (!road.IsComplete())
            return "Unknown model" + '\n';
        return road.Manufacturer + " " + road.Model + "\n" +
               "\n" +
               "Requested action: " + road.RequestedAction + "\n" +
               "Current action: " + road.CurrentAction + "\n" +
               "Priority: " + road.Priority + "\n";
    }

    /**
     * Returns the text of an empty road label
     */
    string MakeCarLabelText()
    {
        return " " + '\n';
    }


    /**
     * Change size of UI element during a resize operation
     * Method called by the window when a SizeAllocated event occurs
     */
    internal void OnResize(Gdk.Rectangle allocation)
    {
        // Update sizes
        cRadW = ScaleValue(cFullRadW, allocation, true);
        cRadH = ScaleValue(cFullRadH, allocation, false);
        labelW = ScaleValue(labelFullW, allocation, true);
        labelH = ScaleValue(labelFullH, allocation, false);
        fontSize = ScaleValue(fontFullSize, allocation, false);
        var newFontDesc = Pango.FontDescription.FromString(fontFamily + " " + fontSize);

        // Crossroad
        if (crossroadImage != null)
        {
            crossroadImage.SizeAllocate(allocation);
            crossroadImage.Pixbuf = crossroadPixbuf.ScaleSimple(allocation.Width, allocation.Height, Gdk.InterpType.Nearest);
        }

        // Cars and labels
        if (roads != null)
        {
            try
            {
                foreach (RoadID road in roads.Keys)
                {
                    var r = roads[road];
                    Image car = r.Item1;
                    Label label = r.Item2;
                    Image leftSignal = r.Item3;
                    Image rightSignal = r.Item4;
                    Image aliveMarker = r.Item5;

                    if (car != null)
                    {
                        Tuple<int, int> pos = ComputeCarPosition(road, car, allocation);
                        container.Move(car, pos.Item1, pos.Item2); //TODO SIZE CAR
                    }

                    if (label != null)
                    {
                        label.SetSizeRequest(labelW, labelH);
                        label.ModifyFont(newFontDesc);

                        Tuple<int, int> pos = ComputeLabelPosition(road, allocation);
                        container.Move(label, pos.Item1, pos.Item2);
                    }

                    if (leftSignal != null && rightSignal != null)
                    {
                        Tuple<int, int, int, int> pos = ComputeSignalPositions(road, leftSignal, allocation);
                        container.Move(leftSignal, pos.Item1, pos.Item2);
                        container.Move(rightSignal, pos.Item3, pos.Item4);
                    }

                    if (aliveMarker != null)
                    {
                        Tuple<int, int> pos = ComputeAliveMarkerPosition(road, aliveMarker, label);
                        container.Move(aliveMarker, pos.Item1, pos.Item2);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // We are here because Roads has been modified by the thread reading on the serial port.
                // We just stop updating the interface, we'll get another resize event soon
                Console.WriteLine("OnResize operation aborted: `Roads` has been modified");
            }
        }
    }

    /**
     * Given a value, it scales to the current window size
     */
    int ScaleValue(int v, Gdk.Rectangle allocation, bool useWidth)
    {
        return useWidth ? (int)Math.Round((float)v * allocation.Width / cFullW, 0)
                        : (int)Math.Round((float)v * allocation.Height / cFullH, 0);
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        if (Monitor != null)
        {
            Monitor.StopSerialPortReading();
        }

        Application.Quit();
        a.RetVal = true;
    }
}