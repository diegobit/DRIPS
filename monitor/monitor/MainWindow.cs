using System;
using System.Collections.Generic;
using Gtk;
using monitor;

public partial class MainWindow : Gtk.Window
{
	// FIXED WINDOW PARAMETERS //TODO: that's shit
	int width = 1364;
	int height = 720;

	string resDiv = "_";
	string imageExtension = ".png";
	string unknownImagePath;

	Fixed container;
	TextView actionText; // The textview with the text with the actions to show;
	Image crossroadImage;
	Dictionary<RoadID, Image> roads;



	public MainWindow() : base(Gtk.WindowType.Toplevel)
	{
		Build();

		Resize(width, height);

		// The global container
		container = new Fixed();
		roads = new Dictionary<RoadID, Image>();
		//ModifyBase(StateType.Normal, new Gdk.Color(255,255,255));
		//container.ModifyBase(StateType.Normal, new Gdk.Color(255, 255, 255));

		// Create Widgets to put into the Fixed container

		crossroadImage = Image.LoadFromResource("monitor.resources.crossroad.png");
		crossroadImage.RedrawOnAllocate = true;
		unknownImagePath = "monitor.resources.car" + resDiv + "Unknown" + resDiv + "Unknown" + imageExtension;

		actionText = new TextView();
		actionText.Editable = false;
		actionText.SetSizeRequest(505, 195);
		actionText.ModifyBase(StateType.Normal, new Gdk.Color(230, 230, 230));
		actionText.ModifyFont(Pango.FontDescription.FromString("Arial 20"));
		actionText.Buffer.Text = "I'm a cereal Listener\n";

		// Put the Widgets inside the container
		container.Put(crossroadImage, 0, 0);
		container.Put(actionText, width / 2 + 160, 5);

		// Put the container in the window
		Add(container);
	}



	public void UpdateRoad(Road road)
	{
		Gtk.Application.Invoke(delegate
		{
			actionText.Buffer.Text += road.Id + " " + road.Manufacturer + '\n';
		});

		Image car;
 		string expectedImagePath = "monitor.resources.car" + resDiv + road.Manufacturer + resDiv + road.Model + imageExtension;
		if (roads.TryGetValue(road.Id, out car))
		{
			// Image present, I check whether I have to update its image
			string prevPath = car.Name;
			if (prevPath != (road.Id + expectedImagePath))
			{
				removeCar(prevPath);
				roads.Remove(road.Id);

				car = loadCarImage(expectedImagePath, road.Id);
				roads.Add(road.Id, car);
				placeCar(car, road);
			}
		}
		else
		{
			car = loadCarImage(expectedImagePath, road.Id);
			roads.Add(road.Id, car);
			placeCar(car, road);
 		}
	}



	Image loadCarImage(string expectedImagePath, RoadID id)
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

	void removeCar(string imageName)
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

	void placeCar(Image car, Road road)
	{
		int crWidth = crossroadImage.Allocation.Width;
		int crHeight = crossroadImage.Allocation.Height;

		Gtk.Application.Invoke(delegate
		{
			switch (road.Id)
			{
			case RoadID.Bottom:
				container.Put(car, crWidth / 2 + 10, crHeight / 2 + 200);
				break;
			case RoadID.Left:
				car.Pixbuf = car.Pixbuf.RotateSimple(Gdk.PixbufRotation.Clockwise);
				container.Put(car, crWidth / 2 - 200, crHeight / 2);
				break;
			case RoadID.Top:
				car.Pixbuf = car.Pixbuf.RotateSimple(Gdk.PixbufRotation.Upsidedown);
				container.Put(car, crWidth / 2 - car.Pixbuf.Width - 10, crHeight / 2 - 200);
				break;
			case RoadID.Right:
				car.Pixbuf = car.Pixbuf.RotateSimple(Gdk.PixbufRotation.Counterclockwise);
				container.Put(car, crWidth / 2 + 200, crHeight / 2 - car.Pixbuf.Height);
				break;
			}
			container.ShowAll();
		});
	}



	protected void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		Application.Quit();
		a.RetVal = true;
	}
}