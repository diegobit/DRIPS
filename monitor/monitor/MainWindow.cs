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

		//RedrawOnAllocate = true;
		//Resize(800, 600);
		//Allocation.Width = 800;
		//Allocation.Height = 600;

		// The global container
		container = new Fixed();

		// Create Widgets to put into the Fixed container
		actionText = new TextView();
		//actionText.SetSizeRequest(100, 100);
		actionText.Buffer.Text = "I'm a cereal Listener\n";
		crossroadImage = Image.LoadFromResource("monitor.resources.crossroad2.png");
		crossroadImage.RedrawOnAllocate = true;
		Console.Write(Allocation.Width);
		Console.Write(Allocation.Height);
		crossroadImage.SetSizeRequest(Allocation.Width, Allocation.Height);

		//crossroadImage.File = "resources/crossroad.png";

		// Put the Widgets inside the container
		container.Put(crossroadImage, 0, 0);
		container.Put(actionText, 50, 10);

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

	public void UpdateActionText(String text)
	{
		actionText.Buffer.Text = text;
		ShowAll();
	}
}
