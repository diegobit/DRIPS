using System;
using Gtk;
using monitor;

public partial class MainWindow : Gtk.Window
{
	// FIXED WINDOW PARAMETERS //TODO: that's shit
	int width = 1364;
	int height = 720;

	string imageExtension = ".png";

	Fixed container;
	TextView actionText; // The textview with the text with the actions to show;
	Image crossroadImage;

	public MainWindow() : base(Gtk.WindowType.Toplevel)
	{
		Build();

		Resize(width, height);

		// The global container
		container = new Fixed();
		//ModifyBase(StateType.Normal, new Gdk.Color(255,255,255));
		//container.ModifyBase(StateType.Normal, new Gdk.Color(255, 255, 255));

		// Create Widgets to put into the Fixed container
		crossroadImage = Image.LoadFromResource("monitor.resources.crossroad.png");
		crossroadImage.RedrawOnAllocate = true;

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

	protected void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		Application.Quit();
		a.RetVal = true;
	}


	//protected override void OnSizeAllocated(Gdk.Rectangle allocation)
	//{
	//	base.OnSizeAllocated(allocation);

	//	if (container != null)
	//	{
	//		Child.SetSizeRequest(allocation.Width, allocation.Height);
	//		//foreach (Widget c in container.Children)
	//		//{
	//		//	Console.Write(c.GetType());
	//		//}
	//	}
	//}

	//protected override void OnSizeRequested(ref Requisition requisition)
	//{
	//	base.OnSizeRequested(ref requisition);
	//	Console.Write("OnSizeRequested");
	//}
}