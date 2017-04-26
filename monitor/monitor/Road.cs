namespace monitor
{
	public class Road
	{
		public RoadID Id { get; private set; }

		public string Manufacturer { get; set; } = "";
		public string Model { get; set; } = "";
		public int Orientation { get; set; } = -1;
		public ReqAction RequestedAction { get; set; } = ReqAction.None;
		public CurrAction CurrentAction { get; set; } = CurrAction.None;



		public Road(RoadID id)
		{
			Id = id;
		}



		/*
		 * Returns true if the road is empty, false if there's a car
		 */
		public bool IsEmpty()
		{
			return Manufacturer == "" & Model == "" && Orientation == -1 &&
                CurrentAction == CurrAction.None && RequestedAction == ReqAction.None;
		}

		/*
		 * Returns true if there's a car on the road, but I only know it's orientation
		 */
		public bool IsPartial()
		{
			return Manufacturer == "" && Model == "" && Orientation != -1 &&
                CurrentAction == CurrAction.None && RequestedAction == ReqAction.None;
		}

		/*
		 * Returns true if there's a car on the road, and I have all the informations about it
		 */
		public bool IsComplete()
		{
			return Manufacturer != "" && Model != "" && Orientation != -1 &&
                CurrentAction != CurrAction.None && RequestedAction != ReqAction.None;
		}



		public void RemoveCar()
		{
			Manufacturer = null;
			Model = null;
			Orientation = -1;
			CurrentAction = 0;
			RequestedAction = 0;
		}
	}
}
