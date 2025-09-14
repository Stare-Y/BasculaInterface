using System.Text.Json;

namespace Core.Application.Settings
{
    public class WeightSettings
    {
        public string BasculaPort { get; set; } = "COM1";
        public int BasculaBauds { get; set; } = 9600;
        public string PrinterName { get; set; } = "POS-80C";
        public bool NeedManualAsk { get; set; } = false;
        public int TimerElapse { get; set; } = 750;
        public string AskChar { get; set; } = "P";
        public int PrintFontSize { get; set; } = 16;
        public int AcceptableDifference { get; set; } = 500;
    }
}
