using BasculaTerminalApi.Models;
using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.Versioning;

namespace BasculaTerminalApi.Service
{
    public class PrintService
    {
        private readonly PrintSettings _printSettings;
        public PrintService(PrintSettings printSettings)
        {
            _printSettings = printSettings;
        }

        [SupportedOSPlatform("windows")]
        public async Task Print(string text)
        {
            string fechaHora = DateTime.Now.ToString("dd-MM-yyyy\nHH:mm:ss");

            PrintDocument document = new();
            document.PrinterSettings.PrinterName = _printSettings.PrinterName;

            document.PrintPage += (sender, e) =>
            {
                if (e.Graphics != null)
                {
                    System.Drawing.Font font = new("Courier New", _printSettings.PrintFontSize);
                    e.Graphics.DrawString(text, font, Brushes.Black, 10, 10);
                }
            };

            await Task.Run(() => document.Print());
        }
    }
}
