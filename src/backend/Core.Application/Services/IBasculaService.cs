using Core.Domain.Events;

namespace Core.Application.Services
{
    public interface IBasculaService
    {
        event EventHandler<OnBasculaReadEventArgs>? OnBasculaRead;
    }
}
