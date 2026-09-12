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
        public int NormalFontSize { get; set; } = 16;
        public int SmallFontSize { get; set; } = 13;
        public int TitleFontSize { get; set; } = 20;
        public int SubTitleFontSize { get; set; } = 18;
        public int AcceptableDifference { get; set; } = 500;
        public required string CompanyName { get; set; }
        public float TicketWidth { get; set; } = 226.716f;
        public float TicketHeight { get; set; } = 841.8897f;
        public string WeightSerialRegex { get; set; } = @"[+-]?\d+(\.\d+)?";
    }
}
