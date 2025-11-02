using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.Extensions.Logging;
using ServicioSincArrendamiento.Models;
using iText.Kernel.Geom;

namespace ServicioSincArrendamiento.Services
{
    public class ReportingService : IReportingService
    {
        private readonly ILogger<ReportingService> _logger;

        public ReportingService(ILogger<ReportingService> logger)
        {
            _logger = logger;
        }

        public Task<byte[]> GenerateMorosidadPdfAsync(IEnumerable<MorosidadInfo> data, byte[]? logoBytes)
        {
            _logger.LogInformation("Iniciando generación de reporte PDF de morosidad para {DataCount} registros.", data.Count());

            using var memoryStream = new MemoryStream();
            var writer = new PdfWriter(memoryStream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, PageSize.LETTER.Rotate());
            document.SetMargins(25, 25, 25, 25);

            var fontBold = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);
            var fontNormal = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);
            var colorVerde = new DeviceRgb(169, 208, 142);
            var colorNaranja = new DeviceRgb(244, 176, 132);
            var colorRojo = new DeviceRgb(247, 98, 69);

            if (logoBytes != null && logoBytes.Length > 0)
            {
                try
                {
                    var logo = new Image(ImageDataFactory.Create(logoBytes));
                    // Fijamos solo la altura, el ancho se ajustará automáticamente para mantener la proporción.
                    logo.SetHeight(43); 
                    document.Add(logo);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo procesar el logo desde los bytes. El reporte se generará sin él.");
                }
            }

            document.Add(new Paragraph("Arrendamiento POS, Morosidad de Debitaciones")
                .SetFont(fontBold).SetFontSize(14).SetTextAlignment(TextAlignment.CENTER));
            document.Add(new Paragraph($"Fecha Creación: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                .SetFont(fontNormal).SetFontSize(12).SetTextAlignment(TextAlignment.LEFT));
            document.Add(new Paragraph("Detalle de comercios con morosidad en la debitación de Arrendamiento de Equipo Pos.")
                .SetFont(fontNormal).SetFontSize(12).SetTextAlignment(TextAlignment.JUSTIFIED));
            document.Add(new Paragraph(" "));

            if (!data.Any())
            {
                document.Add(new Paragraph("No se encontraron datos de morosidad para el período seleccionado.")
                    .SetFont(fontBold).SetFontSize(12).SetTextAlignment(TextAlignment.CENTER));
                document.Close();
                return Task.FromResult(memoryStream.ToArray());
            }

            var groupedData = data.GroupBy(d => d.Pte switch {
                1 => 1,
                2 => 2,
                _ => 3
            }).OrderBy(g => g.Key);

            string[] headers = { "No", "Banco", "Retailer", "Nombre", "Monto", "Saldo", "Pte", "Inicio", "Mes", "Pos", "Estado", "Abonos", "DebC", "DebA", "Max" };
            float[] columnWidths = { 20, 25, 45, 80, 30, 30, 20, 35, 30, 20, 35, 30, 30, 30, 30 };

            foreach (var grupo in groupedData)
            {
                string titulo;
                Color colorHeader;
                switch (grupo.Key)
                {
                    case 1:
                        titulo = "Comercios con 30 días de Incobrabilidad";
                        colorHeader = colorVerde;
                        break;
                    case 2:
                        titulo = "Comercios con 60 días de Incobrabilidad";
                        colorHeader = colorNaranja;
                        break;
                    default:
                        titulo = "Comercios con 90 días o más de Incobrabilidad";
                        colorHeader = colorRojo;
                        break;
                }

                document.Add(new Paragraph(titulo).SetFont(fontBold).SetFontSize(11));
                document.Add(new Paragraph(" "));

                var table = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();

                foreach (var header in headers)
                {
                    table.AddHeaderCell(new Cell().Add(new Paragraph(header))
                        .SetFont(fontBold).SetFontSize(10).SetBackgroundColor(colorHeader)
                        .SetTextAlignment(TextAlignment.CENTER).SetVerticalAlignment(VerticalAlignment.MIDDLE));
                }

                foreach (var item in grupo)
                {
                    table.AddCell(new Cell().Add(new Paragraph(item.No.ToString())).SetFont(fontNormal).SetFontSize(8));
                    table.AddCell(new Cell().Add(new Paragraph(item.Banco)).SetFont(fontNormal).SetFontSize(8));
                    table.AddCell(new Cell().Add(new Paragraph(item.Retailer)).SetFont(fontNormal).SetFontSize(8));
                    table.AddCell(new Cell().Add(new Paragraph(item.Nombre)).SetFont(fontNormal).SetFontSize(8));
                    table.AddCell(new Cell().Add(new Paragraph(item.Monto.ToString("N2"))).SetFont(fontNormal).SetFontSize(8));
                    table.AddCell(new Cell().Add(new Paragraph(item.Saldo.ToString("N2"))).SetFont(fontNormal).SetFontSize(8));
                    table.AddCell(new Cell().Add(new Paragraph(item.Pte.ToString())).SetFont(fontNormal).SetFontSize(8));
                    table.AddCell(new Cell().Add(new Paragraph(item.Inicio.ToString("dd/MM/yyyy"))).SetFont(fontNormal).SetFontSize(8));
                    table.AddCell(new Cell().Add(new Paragraph(item.Mes)).SetFont(fontNormal).SetFontSize(8));
                    table.AddCell(new Cell().Add(new Paragraph(item.Pos.ToString())).SetFont(fontNormal).SetFontSize(8));
                    table.AddCell(new Cell().Add(new Paragraph(item.Estado)).SetFont(fontNormal).SetFontSize(8));
                    table.AddCell(new Cell().Add(new Paragraph(item.Abonos.ToString("N2"))).SetFont(fontNormal).SetFontSize(8));
                    table.AddCell(new Cell().Add(new Paragraph(item.DebC.ToString("N2"))).SetFont(fontNormal).SetFontSize(8));
                    table.AddCell(new Cell().Add(new Paragraph(item.DebA.ToString("N2"))).SetFont(fontNormal).SetFontSize(8));
                    table.AddCell(new Cell().Add(new Paragraph(item.Max.ToString("N2"))).SetFont(fontNormal).SetFontSize(8));
                }

                document.Add(table);
                document.Add(new Paragraph(" "));
            }

            document.Close();
            _logger.LogInformation("Reporte PDF de morosidad generado exitosamente en memoria.");
            return Task.FromResult(memoryStream.ToArray());
        }

        public Task<byte[]> GenerateMorosidadExcelAsync(IEnumerable<MorosidadInfo> data)
        {
            _logger.LogInformation("Iniciando generación de reporte Excel de morosidad para {DataCount} registros.", data.Count());
            using var workbook = new XLWorkbook();

            // Agrupar los datos como en el reporte PDF
            var groupedData = data.GroupBy(d => d.Pte switch
            {
                1 => 1, // 30 dias
                2 => 2, // 60 dias
                _ => 3  // 90+ dias
            }).OrderByDescending(g => g.Key); // 90, 60, 30

            string[] headers = { 
                "No", "Banco", "Retailer", "Comercio", "Valor de Arrendamiento", "Saldo",
                "Cantidad de Meses Pendientes de liquidar", "Inicio", "Mes Pendiente de cobrar a la fecha",
                "Cantidad de POS", "Estado", "Abonos al comercio", "Debitos por Contracargos", 
                "Debitos por Arrendamiento", "Pago Maximo en el mes"
            };

            foreach (var grupo in groupedData)
            {
                string sheetName;
                switch (grupo.Key)
                {
                    case 1: sheetName = "30 dias"; break;
                    case 2: sheetName = "60 dias"; break;
                    default: sheetName = "90 dias"; break;
                }

                var worksheet = workbook.Worksheets.Add(sheetName);

                // Escribir cabeceras
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                }

                // Aplicar estilo a la fila de cabecera
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
                headerRow.Style.Font.FontColor = XLColor.White;
                headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Escribir datos
                int rowIdx = 2;
                foreach (var item in grupo.OrderBy(i => i.No))
                {
                    worksheet.Cell(rowIdx, 1).Value = item.No;
                    worksheet.Cell(rowIdx, 2).Value = item.Banco;
                    worksheet.Cell(rowIdx, 3).Value = item.Retailer;
                    worksheet.Cell(rowIdx, 4).Value = item.Nombre;
                    worksheet.Cell(rowIdx, 5).Value = item.Monto;
                    worksheet.Cell(rowIdx, 6).Value = item.Saldo;
                    worksheet.Cell(rowIdx, 7).Value = item.Pte;
                    worksheet.Cell(rowIdx, 8).Value = item.Inicio;
                    worksheet.Cell(rowIdx, 9).Value = item.Mes;
                    worksheet.Cell(rowIdx, 10).Value = item.Pos;
                    worksheet.Cell(rowIdx, 11).Value = item.Estado;
                    worksheet.Cell(rowIdx, 12).Value = item.Abonos;
                    worksheet.Cell(rowIdx, 13).Value = item.DebC;
                    worksheet.Cell(rowIdx, 14).Value = item.DebA;
                    worksheet.Cell(rowIdx, 15).Value = item.Max;
                    rowIdx++;
                }

                // Aplicar formatos a las columnas
                worksheet.Column(3).Style.NumberFormat.Format = "@"; // Retailer como Texto
                worksheet.Column(5).Style.NumberFormat.Format = "$#,##0.00"; // Monto
                worksheet.Column(6).Style.NumberFormat.Format = "$#,##0.00"; // Saldo
                worksheet.Column(8).Style.NumberFormat.Format = "dd/mm/yyyy"; // Inicio
                worksheet.Range(worksheet.Cell(2, 12), worksheet.Cell(rowIdx, 15)).Style.NumberFormat.Format = "$#,##0.00"; // Abonos a Max

                worksheet.Columns().AdjustToContents();
            }


            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            _logger.LogInformation("Reporte Excel de morosidad multi-hoja generado exitosamente.");
            return Task.FromResult(stream.ToArray());
        }

        public Task<byte[]> GenerateInactivosExcelAsync(IEnumerable<InactivoInfo> data)
        {
            _logger.LogInformation("Iniciando generación de reporte Excel de inactivos para {DataCount} registros.", data.Count());
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Inactivos");

            string[] headers = { 
                "No", "Banco", "Retailer", "Comercio", "Valor de Arrendamiento", "Saldo",
                "Cantidad de Meses Pendientes de liquidar", "Inicio", "Fecha Desactivacion", 
                "Cantidad de POS", "Estado" 
            };

            // Escribir cabeceras
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            // Aplicar estilo a la fila de cabecera
            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4"); // Azul estándar de Office
            headerRow.Style.Font.FontColor = XLColor.White;
            headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int rowIdx = 2;
            foreach (var item in data)
            {
                worksheet.Cell(rowIdx, 1).Value = item.No;
                worksheet.Cell(rowIdx, 2).Value = item.Banco;
                worksheet.Cell(rowIdx, 3).Value = item.Retailer;
                worksheet.Cell(rowIdx, 4).Value = item.Nombre;
                worksheet.Cell(rowIdx, 5).Value = item.Monto;
                worksheet.Cell(rowIdx, 6).Value = item.Saldo;
                worksheet.Cell(rowIdx, 7).Value = item.Pte;
                worksheet.Cell(rowIdx, 8).Value = item.Inicio;
                worksheet.Cell(rowIdx, 9).Value = item.Retiro;
                worksheet.Cell(rowIdx, 10).Value = item.Pos;
                worksheet.Cell(rowIdx, 11).Value = item.Estado;
                rowIdx++;
            }

            // Aplicar formatos a las columnas
            worksheet.Column(3).Style.NumberFormat.Format = "@"; // Retailer como Texto
            worksheet.Column(5).Style.NumberFormat.Format = "$#,##0.00"; // Monto
            worksheet.Column(6).Style.NumberFormat.Format = "$#,##0.00"; // Saldo
            worksheet.Column(8).Style.NumberFormat.Format = "dd/mm/yyyy"; // Inicio
            worksheet.Column(9).Style.NumberFormat.Format = "dd/mm/yyyy"; // Retiro

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            _logger.LogInformation("Reporte Excel de inactivos generado exitosamente.");
            return Task.FromResult(stream.ToArray());
        }
    }
} 