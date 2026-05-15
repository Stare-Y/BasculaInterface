using Core.Domain.Entities.Base;
using Core.Domain.Enums;

namespace Core.Domain.Entities.Terminals
{
    public class Terminal : BaseEntity
    {
        public required string Name { get; set; }
        public TerminalMode Mode { get; set; } = TerminalMode.NotConfigured;
        public required string DefaultPrinterName { get; set; }
    }
}
