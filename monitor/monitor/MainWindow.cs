using System;
using Gtk;
using monitor;

public partial class MainWindow : Gtk.Window
{
	private Monitor monitor;
	private Label actionLabel; // The label with the text with the actions to show;

	public MainWindow(Monitor m) : base(Gtk.WindowType.Toplevel)
	{
		monitor = m;

		actionLabel = new Label();
		actionLabel.Text = "I'm a cereal Listener\n"; //TODO

		Build();

		VBox vb = (VBox)Child;
		HBox hb_top = (HBox)vb.Children[0];
		HBox hb_bottom = (HBox)vb.Children[1];
		hb_top.Add(actionLabel);
	}

	protected void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		Application.Quit();
		a.RetVal = true;
	}

	public void UpdateActionText(String text)
	{
		actionLabel.Text = text;
		ShowAll();
	}
}
