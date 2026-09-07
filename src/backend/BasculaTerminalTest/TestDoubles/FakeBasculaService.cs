using Core.Application.Services;
using Core.Domain.Events;

namespace BasculaTerminalTest.TestDoubles
{
    /// <summary>
    /// Stand-in for the real <see cref="Infrastructure.Service.BasculaService"/>, which opens a
    /// physical serial port in its constructor and so can't run without hardware. Tests call
    /// <see cref="RaiseWeight"/> to push a deterministic reading through the exact same
    /// <see cref="IBasculaService.OnBasculaRead"/> event the <c>SerialPortHub</c> subscribes to.
    /// </summary>
    public sealed class FakeBasculaService : IBasculaService
    {
        public event EventHandler<OnBasculaReadEventArgs>? OnBasculaRead;

        public void RaiseWeight(double weight) => OnBasculaRead?.Invoke(this, new OnBasculaReadEventArgs(weight));

        public bool HasSubscribers => OnBasculaRead is not null;
    }
}
