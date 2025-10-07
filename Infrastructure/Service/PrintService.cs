using Core.Application.DTOs;
using Core.Application.Services;
using Core.Application.Settings;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Infrastructure.Service
{
    public class PrintService : IPrintService
    {
        private readonly WeightSettings _settings;
        private readonly ILogger<PrintService> _logger;
        private readonly IClienteProveedorService _clienteService;
        private readonly IProductService _productService;

        private readonly PdfFont _regularFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.COURIER);
        private readonly PdfFont _boldFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.COURIER_BOLD);
        public PrintService(IOptions<WeightSettings> printSettings, ILogger<PrintService> logger, IClienteProveedorService clienteService, IProductService productService)
        {
            _settings = printSettings.Value;
            _logger = logger;
            _clienteService = clienteService;
            _productService = productService;
        }

        public async Task Print(WeightEntryDto entry)
        {
            try
            {
                iText.Kernel.Geom.PageSize pageSize = new(_settings.TicketWidth, _settings.TicketHeight);
                // Create a temp PDF
                string filePath = Path.GetTempFileName() + ".pdf";

                using (PdfWriter writer = new(filePath))
                using (PdfDocument pdf = new(writer))
                using (Document document = new(pdf, pageSize))
                {
                    //document content
                    await BuildWeightEntryDocument(document, entry);

                    PdfPage page = pdf.GetFirstPage();
                    float contentHeight = document.GetRenderer().GetCurrentArea().GetBBox().GetY();
                    float usedHeight = _settings.TicketHeight - contentHeight;
                    page.SetMediaBox(new iText.Kernel.Geom.Rectangle(0, 0, _settings.TicketWidth, usedHeight));
                }
                PrintPdf(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing");
                throw new InvalidOperationException("Hubo un error al imprimir el ticket.", ex);
            }
        }

        public void Print(string text)
        {
            try
            {
                iText.Kernel.Geom.PageSize pageSize = new(_settings.TicketWidth, _settings.TicketHeight);

                // Create a temp PDF
                string filePath = Path.GetTempFileName() + ".pdf";

                using (PdfWriter writer = new(filePath))
                using (PdfDocument pdf = new(writer))
                using (Document document = new(pdf, pageSize))
                {
                    document.SetMargins(20f, 20f, 0, 0);

                    var fontSize = _settings.NormalFontSize;
                    var fechaHora = DateTime.Now.ToString("dd-MM-yyyy\nHH:mm:ss");
                    document.Add(new Paragraph(fechaHora + " " + text).SetFontSize(fontSize).SetFont(iText.Kernel.Font.PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.COURIER)));

                    PdfPage page = pdf.GetFirstPage();
                    float contentHeight = document.GetRenderer().GetCurrentArea().GetBBox().GetY();
                    float usedHeight = _settings.TicketHeight - contentHeight;

                    // Ajustar tamaño real de la página al contenido
                    page.SetMediaBox(new iText.Kernel.Geom.Rectangle(0, 0, _settings.TicketWidth, usedHeight));
                }

                PrintPdf(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing plain text");
                return;
            }
        }

        private void PrintPdf(string filePath)
        {
            _logger.LogInformation($"Preparing to print file: {filePath}");
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
            string arguments = $"-print-to \"{_settings.PrinterName}\" \"{filePath}\" -silent -exit-on-print";

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
                _logger.LogInformation("Print command executed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during printing process");
            }
        }

        private async Task BuildWeightEntryDocument(Document document, WeightEntryDto entry)
        {
            document.SetMargins(0, 10f, 10f, 10f);

            document.Add(await BuildWeightHeader(entry));

            document.Add(await BuildWeightDetailsTable(entry));
        }

        private async Task<Table> BuildWeightDetailsTable(WeightEntryDto entry)
        {
            Table table = new(5);// 5 columns

            table.AddCell(new Cell(1, 5)
                .Add(BuildParagraph()));// Empty row

            table.AddCell(new Cell(1, 5).SetBorder(Border.NO_BORDER)
                .Add(BuildParagraph("Productos de pedido:")));

            foreach (WeightDetailDto detail in entry.WeightDetails)
            {
                string productName = "Pesada libre";

                if (detail.FK_WeightedProductId.HasValue)
                {
                    try
                    {
                        ProductoDto product = await _productService
                            .GetByIdAsync(detail.FK_WeightedProductId
                            ?? throw new InvalidDataException("The product Id is null"));

                        if (product != null)
                        {
                            productName = product.Nombre;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error fetching product info for printing");
                    }
                }

                table.AddCell(new Cell(2, 5).SetBorder(Border.NO_BORDER)
                    .Add(BuildParagraph(productName, _settings.SubTitleFontSize, TextAlignment.CENTER, bold: true)));

                if (detail.RequiredAmount.HasValue)
                {
                    table.AddCell(new Cell(1, 5).SetBorder(Border.NO_BORDER)
                        .Add(BuildParagraph($"Solicitado: {detail.RequiredAmount}kg", _settings.SmallFontSize, TextAlignment.LEFT)));
                }

                table.AddCell(new Cell().SetBorder(Border.NO_BORDER)
                    .Add(BuildParagraph($"Peso anterior:", _settings.SmallFontSize)));

                table.AddCell(new Cell(1, 4).SetBorder(Border.NO_BORDER)
                    .Add(BuildParagraph(detail.Tare.ToString("F2") + "kg", _settings.SmallFontSize, TextAlignment.RIGHT, bold: false)));

                table.AddCell(new Cell().SetBorder(Border.NO_BORDER)
                    .Add(BuildParagraph("Cargado:", _settings.SmallFontSize)));

                table.AddCell(new Cell(1, 4).SetBorder(Border.NO_BORDER)
                        .Add(BuildParagraph(detail.Weight.ToString("F2") + "kg", _settings.SubTitleFontSize, TextAlignment.RIGHT, bold: true)));

                table.AddCell(new Cell(1, 5).SetBorder(Border.NO_BORDER)
                .Add(BuildParagraph()));// Empty row
            }

            table.AddCell(new Cell(1, 5)
                .Add(BuildParagraph()));// Empty row

            table.AddCell(new Cell(1, 3).SetBorder(Border.NO_BORDER)
                .Add(BuildParagraph("Total:", _settings.NormalFontSize, TextAlignment.RIGHT)));
            table.AddCell(new Cell(1, 2).SetBorder(Border.NO_BORDER)
                .Add(BuildParagraph(entry.BruteWeight.ToString("F2") + "kg", _settings.SubTitleFontSize, TextAlignment.RIGHT, true)));

            return table;
        }

        private Paragraph BuildParagraph(string text = "", int fontSize = 16, TextAlignment textAlignment = TextAlignment.LEFT, bool bold = false)
        {
            return new Paragraph(text)
                .SetFontSize(fontSize)
                .SetFont(bold ? _boldFont : _regularFont)
                .SetTextAlignment(textAlignment);
        }
        private async Task<Table> BuildWeightHeader(WeightEntryDto entry)
        {
            Table table = new(5);// 5 columns

            table.AddCell(new Cell(1, 5).SetBorder(Border.NO_BORDER)
                .Add(BuildParagraph(_settings.CompanyName, _settings.TitleFontSize, TextAlignment.CENTER, true)));

            table.AddCell(new Cell(1, 5).SetBorder(Border.NO_BORDER)
                .Add(BuildParagraph("Detalle de Pedido", _settings.SubTitleFontSize, TextAlignment.CENTER)));

            table.AddCell(new Cell(1, 5).SetBorder(Border.NO_BORDER)
                .Add(BuildParagraph()));// Empty row

            //partner
            if (entry.PartnerId.HasValue)
            {
                try
                {
                    ClienteProveedorDto partner = await _clienteService.GetById(entry.PartnerId.Value);

                    table.AddCell(new Cell(1, 5).SetBorder(Border.NO_BORDER)
                        .Add(BuildParagraph("Socio:")));
                    table.AddCell(new Cell(2, 5).SetBorder(Border.NO_BORDER)
                        .Add(BuildParagraph(partner.RazonSocial, bold: true)));
                    table.AddCell(new Cell(1, 5).SetBorder(Border.NO_BORDER)
                        .Add(BuildParagraph()));// Empty row
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching partner info for printing");
                }
            }

            //plate
            table.AddCell(new Cell(1, 5).SetBorder(Border.NO_BORDER)
                .Add(BuildParagraph("Placas:")));
            table.AddCell(new Cell(1, 5).SetBorder(Border.NO_BORDER)
                .Add(BuildParagraph(entry.VehiclePlate, bold: true)));
            table.AddCell(new Cell(1, 5).SetBorder(Border.NO_BORDER)
                .Add(BuildParagraph()));// Empty row

            table.AddCell(new Cell(1, 5)
                .Add(BuildParagraph()));// Empty row

            table.AddCell(new Cell(1, 1).SetBorder(Border.NO_BORDER)
            .Add(BuildParagraph("Peso inicial:")));

            table.AddCell(new Cell(1, 4).SetBorder(Border.NO_BORDER)
                .Add(BuildParagraph(entry.TareWeight.ToString("F2") + "kg", textAlignment: TextAlignment.RIGHT, bold: true)));

            if (entry.CreatedAt.HasValue)
            {
                DateTime created = entry.CreatedAt.Value;
                //created
                table.AddCell(new Cell(1, 5).SetBorder(Border.NO_BORDER)
                    .Add(BuildParagraph("Llegada:")));
                table.AddCell(new Cell(1, 5).SetBorder(Border.NO_BORDER)
                    .Add(BuildParagraph(created.ToLocalTime().ToString("dd-MM-yyyy HH:mm:ss"))));
                table.AddCell(new Cell(1, 5).SetBorder(Border.NO_BORDER)
                    .Add(BuildParagraph()));// Empty row
            }

            if (entry.ConcludeDate.HasValue)
            {
                table.AddCell(new Cell(1, 5).SetBorder(Border.NO_BORDER)
                    .Add(BuildParagraph("Salida:")));
                table.AddCell(new Cell(1, 5).SetBorder(Border.NO_BORDER)
                    .Add(BuildParagraph(entry.ConcludeDate.Value.ToString("dd-MM-yyyy HH:mm:ss"))));
                table.AddCell(new Cell(1, 5).SetBorder(Border.NO_BORDER)
                    .Add(BuildParagraph()));// Empty row
            }

            return table;
        }


    }
}
