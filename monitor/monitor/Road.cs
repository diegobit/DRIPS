namespace monitor
{
	public class Road
	{
		public RoadID Id { get; private set; }

		public string Manufacturer { get; set; } = "";
		public string Model { get; set; } = "";
		public int Orientation { get; set; } = -1;
		public Priority Priority { get; set; } = Priority.None;
		public Action RequestedAction { get; set; } = Action.None;
		public Action CurrentAction { get; set; } = Action.None;



		public Road(RoadID id)
		{
			Id = id;
		}



		/*
		 * Returns true if the road id is empty, false if there's a car
		 */
		bool IsEmpty()
		{
			return Manufacturer == null & Model == null && Orientation == -1 &&
				Priority == 0 && CurrentAction == 0 && RequestedAction == 0;
		}

		/*
		 * Returns true if there's a car on the road id, but I only know it's orientation
		 */
		bool IsPartial()
		{
			return Manufacturer == null && Model == null && Orientation != -1 &&
				Priority == 0 && CurrentAction == 0 && RequestedAction == 0;
		}

		/*
		 * Returns true if there's a car on the road id, and I have all the informations about it
		 */
		bool IsComplete()
		{
			return Manufacturer != null && Model != null && Orientation == -1 &&
				Priority != 0 && CurrentAction != 0 && RequestedAction != 0;
		}



		public void RemoveCar()
		{
			Manufacturer = null;
			Model = null;
			Orientation = -1;
			Priority = 0;
			CurrentAction = 0;
			RequestedAction = 0;
		}
	}
}
