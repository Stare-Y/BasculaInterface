using Core.Application.DTOs;

namespace Core.Application.Services
{
    public interface IPrintService
    {
        void Print(string text);
        Task Print(WeightEntryDto entry);
    }
}
