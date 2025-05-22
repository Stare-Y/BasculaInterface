namespace BasculaTerminalApi.Events
{
    public class OnBasculaReadEventArgs : EventArgs
    {
        public double Weight { get; }
        public OnBasculaReadEventArgs(double weight)
        {
            Weight = weight;
        }
    }
}
