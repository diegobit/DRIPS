using System;
using Gtk;
using monitor;

public partial class MainWindow : Gtk.Window
{
	private Fixed container;
	private TextView actionText; // The textview with the text with the actions to show;
	private Image crossroadImage;

	public MainWindow() : base(Gtk.WindowType.Toplevel)
	{
		Build();

		Resize(1364, 720); //TODO: that's shit

		// The global container
		container = new Fixed();

		// Create Widgets to put into the Fixed container
		crossroadImage = Image.LoadFromResource("monitor.resources.crossroad.png");
		crossroadImage.RedrawOnAllocate = true;
		//crossroadImage.SetSizeRequest(720, 720);


		actionText = new TextView();
		actionText.Editable = false;
		actionText.SetSizeRequest(505, 195);
		actionText.Buffer.Text = "I'm a cereal Listener\n";

		//crossroadImage.File = "resources/crossroad.png";

		// Put the Widgets inside the container
		container.Put(crossroadImage, 0, 0);
		container.Put(actionText, 1364/2+160, 5);

		// Put the container in the window
		Add(container);
	}

	protected void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		Application.Quit();
		a.RetVal = true;
	}

	//override protected void OnResizeChecked()
	//{
	//	base.OnResizeChecked();
	//	crossroadImage.SetSizeRequest(Allocation.Width, Allocation.Height);
	//}

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

	public void Update(String text)
	{
		actionText.Buffer.Text += '\n' + text;
		ShowAll();
	}
}
