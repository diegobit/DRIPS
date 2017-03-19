using System;
using Gtk;
using monitor;

public partial class MainWindow : Gtk.Window
{
	private Monitor monitor;

	public MainWindow(Monitor m) : base(Gtk.WindowType.Toplevel)
	{
		monitor = m;
		Build();
	}

	protected void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		Application.Quit();
		a.RetVal = true;
	}

	protected void onButtonClick(object sender, EventArgs e)
	{
		monitor.SaySomething();
	}
}
