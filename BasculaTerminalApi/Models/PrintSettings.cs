using System.Text.Json;

namespace BasculaTerminalApi.Models
{
    public class PrintSettings
    {
        public string BasculaPort { get; set; } = "COM1";
        public int BasculaBauds { get; set; } = 9600;
        public string PrinterName { get; set; } = "POS-80C";
        public bool NeedManualAsk { get; set; } = false;
        public int TimerElapse { get; set; } = 750;
        public string AskChar { get; set; } = "P";
        public int PrintFontSize { get; set; } = 16;
        public int AcceptableDifference { get; set; } = 500;

        public void ReadSettings()
        {
            string settingsFile = Path.Combine(AppContext.BaseDirectory, "Config/settings.json");
            if (!File.Exists(settingsFile))
            {
                throw new InvalidDataException("El archivo settings.json no existe");
            }

            string jsonContent = File.ReadAllText(settingsFile);

            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent);

            if (data is null)
                throw new InvalidDataException("El archivo settings.json parece ser nulo");

            SetIfPresent(data);
        }

        private void SetIfPresent(Dictionary<string, JsonElement> data)
        {
            if (data.TryGetValue("BasculaPort", out var port) && port.ValueKind == JsonValueKind.String)
            {
                var value = port.GetString();
                if (!string.IsNullOrEmpty(value)) BasculaPort = value;
            }

            if (data.TryGetValue("BasculaBauds", out var bauds) && bauds.TryGetInt32(out var baudsValue))
            {
                BasculaBauds = baudsValue;
            }

            if (data.TryGetValue("PrinterName", out var printer) && printer.ValueKind == JsonValueKind.String)
            {
                var value = printer.GetString();
                if (!string.IsNullOrEmpty(value)) PrinterName = value;
            }

            if (data.TryGetValue("NeedManualAsk", out var manualAsk) && manualAsk.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                NeedManualAsk = manualAsk.GetBoolean();
            }

            if (data.TryGetValue("TimerElapse", out var timer) && timer.TryGetInt32(out var timerValue))
            {
                TimerElapse = timerValue;
            }

            if (data.TryGetValue("AskChar", out var askChar) && askChar.ValueKind == JsonValueKind.String)
            {
                var value = askChar.GetString();
                if (!string.IsNullOrEmpty(value)) AskChar = value;
            }

            if (data.TryGetValue("PrintFontSize", out var fontSize) && fontSize.TryGetInt32(out var fontSizeValue))
            {
                PrintFontSize = fontSizeValue;
            }
            if (data.TryGetValue("AcceptableDifference", out var acceptableDifference) && acceptableDifference.TryGetInt32(out var diffValue))
            {
                AcceptableDifference = diffValue;
            }
        }
    }
}
