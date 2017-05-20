namespace monitor
{
    public class Road
    {
        public RoadID Id { get; private set; }

        public bool IsEmpty { get; set; } = true;
        public string Manufacturer { get; set; } = "";
        public string Model { get; set; } = "";
        public int Orientation { get; set; } = -1;
        public Priority Priority { get; set; } = Priority.Normal;
        public RequestedAction RequestedAction { get; set; } = RequestedAction.None;
        public CurrentAction CurrentAction { get; set; } = CurrentAction.None;



        public Road(RoadID id)
        {
            Id = id;
        }



        /*
         * @return true if there is a car on the road, and I know all about it
         */
        public bool IsComplete()
        {
            return !IsEmpty && Manufacturer != "" && Model != "" && Orientation != -1 &&
                CurrentAction != CurrentAction.None && RequestedAction != RequestedAction.None;
        }



        public void RemoveCar()
        {
            IsEmpty = true;
            Manufacturer = null;
            Model = null;
            Orientation = -1;
            Priority = Priority.Normal;
            RequestedAction = RequestedAction.None;
            CurrentAction = CurrentAction.None;
        }
    }
}
