using System;
using Gtk;

namespace monitor
{
	public class RLayout : Layout
	{
		MainWindow window;

		public RLayout(MainWindow w, IntPtr raw) : base(raw)
		{
			window = w;
		}
		public RLayout(MainWindow w, Adjustment hadjustment, Adjustment vadjustment) : base(hadjustment, vadjustment)
		{
			window = w;
		}

		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			if (Allocation.Width != allocation.Width || Allocation.Height != allocation.Height)
			{
				window.OnResize(new Gdk.Rectangle(0, 0, allocation.Width, allocation.Height));
			}
			base.OnSizeAllocated(allocation);
		}
	}
}
