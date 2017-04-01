using System;
namespace monitor
{
	//[System.ComponentModel.ToolboxItem(true)]
	public class ResizableFixed : Gtk.Fixed
	{
		public ResizableFixed()
		{
			//this.Build();

		}

		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			Console.WriteLine("OnSizeAllocated Resizable + w:" + allocation.Width + " h:" + allocation.Height);
			Foreach((widget) => Console.Write(widget.GetType()));
			//foreach (Widget c in Children)
			//{
			//	Console.Write(c.GetType());
			//}
			//base.OnSizeAllocated(allocation);
		}
	}
}
