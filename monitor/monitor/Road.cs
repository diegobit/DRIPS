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
		 * Returns true if the road is empty, false if there's a car
		 */
		bool IsEmpty()
		{
			return Manufacturer == "" & Model == "" && Orientation == -1 &&
                Priority == Priority.None && CurrentAction == Action.None && RequestedAction == Action.None;
		}

		/*
		 * Returns true if there's a car on the road, but I only know it's orientation
		 */
		bool IsPartial()
		{
			return Manufacturer == "" && Model == "" && Orientation != -1 &&
                Priority == Priority.None && CurrentAction == Action.None && RequestedAction == Action.None;
		}

		/*
		 * Returns true if there's a car on the road, and I have all the informations about it
		 */
		bool IsComplete()
		{
			return Manufacturer != "" && Model != "" && Orientation != -1 &&
                Priority != Priority.None && CurrentAction != Action.None && RequestedAction != Action.None;
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
