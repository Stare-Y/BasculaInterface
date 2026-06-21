namespace Core.Domain.Entities.Weight
{
    public class WeightConcurrencyException : Exception
    {
        public WeightConcurrencyException(string message, Exception? inner = null)
            : base(message, inner) { }
    }
}
