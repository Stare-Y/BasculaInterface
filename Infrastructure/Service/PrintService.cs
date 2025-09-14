using Core.Application.Services;
using Core.Application.Settings;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Infrastructure.Service
{
    public class PrintService : IPrintService
    {
        private readonly WeightSettings _printSettings;
        private readonly ILogger _logger;
        public PrintService(IOptions<WeightSettings> printSettings, ILogger<PrintService> logger)
        {
            _printSettings = printSettings.Value;
            _logger = logger;
        }

        public void Print(string text)
        {
            try
            {
                // Create a temp PDF
                string filePath = Path.GetTempFileName() + ".pdf";
                using (var writer = new PdfWriter(filePath))
                using (var pdf = new PdfDocument(writer))
                using (var document = new Document(pdf))
                {
                    var fontSize = _printSettings.PrintFontSize;
                    var fechaHora = DateTime.Now.ToString("dd-MM-yyyy\nHH:mm:ss");
                    document.Add(new Paragraph(fechaHora + " " + text).SetFontSize(fontSize).SetFont(iText.Kernel.Font.PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.COURIER)));
                }

                string cmd = $"copy /B \"{filePath}\" \"{_printSettings.PrinterName}\"";

                // Send it to the printer (Windows PrintTo verb)
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C {cmd}",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false
                };

                Process.Start(processInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing");
                return;
            }
        }
    }
}
