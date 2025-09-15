using Core.Application.Services;
using Core.Application.Settings;
using iText.Kernel.Geom;
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
        private readonly ILogger<PrintService> _logger;
        public PrintService(IOptions<WeightSettings> printSettings, ILogger<PrintService> logger)
        {
            _printSettings = printSettings.Value;
            _logger = logger;
        }

        private void PrintPdf(string filePath)
        {
            string sumatraPath = AppContext.BaseDirectory + "External Dep\\SumatraPDF.exe";

            // Validate that the required files exist.
            if (!File.Exists(sumatraPath))
            {
                throw new FileNotFoundException("SumatraPDF.exe was not found. Please check the path.", sumatraPath);
            }
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The specified PDF file was not found.", filePath);
            }

            // Use string interpolation to build the arguments string.
            // The -print-to and -exit-on-print flags are crucial here.
            string arguments = $"-print-to \"{_printSettings.PrinterName}\" \"{filePath}\" -silent -exit-on-print";

            // Configure the process to run silently without a visible window.
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = sumatraPath,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            try
            {
                // Start the process and wait for it to finish.
                // This ensures the print command is sent before your C# application potentially closes.
                using (Process? process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        throw new InvalidOperationException("Failed to start the printing process.");
                    }
                    process.WaitForExit();
                }
                Console.WriteLine($"Print job for '{filePath}' sent to printer '{_printSettings.PrinterName}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while printing: {ex.Message}");
            }
        }

        public void Print(string text)
        {
            try
            {
                float ticketWidth = 226.716f;
                float ticketHeight = 841.8897f;
                iText.Kernel.Geom.PageSize pageSize = new iText.Kernel.Geom.PageSize(ticketWidth, ticketHeight);
                // Create a temp PDF
                string filePath = System.IO.Path.GetTempFileName() + ".pdf";
                using (var writer = new PdfWriter(filePath))
                using (var pdf = new PdfDocument(writer))
                using (var document = new Document(pdf, pageSize))
                {
                    document.SetMargins(20f, 20f, 0, 0);
                    var fontSize = _printSettings.PrintFontSize;
                    var fechaHora = DateTime.Now.ToString("dd-MM-yyyy\nHH:mm:ss");
                    document.Add(new Paragraph(fechaHora + " " + text).SetFontSize(fontSize).SetFont(iText.Kernel.Font.PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.COURIER)));
                   
                    var page = pdf.GetFirstPage();
                    var contentHeight = document.GetRenderer().GetCurrentArea().GetBBox().GetY();
                    float usedHeight = ticketHeight - contentHeight;

                    // Ajustar tamaño real de la página al contenido
                    page.SetMediaBox(new iText.Kernel.Geom.Rectangle(0, 0, ticketWidth, usedHeight));

                }

                PrintPdf(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing");
                return;
            }
        }
    }
}
