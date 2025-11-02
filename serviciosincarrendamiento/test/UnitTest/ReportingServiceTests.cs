using Microsoft.Extensions.Logging;
using Moq;
using ServicioSincArrendamiento.Models;
using ServicioSincArrendamiento.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnitTest
{
    public class ReportingServiceTests
    {
        private readonly Mock<ILogger<ReportingService>> _mockLogger;
        private readonly ReportingService _reportingService;

        public ReportingServiceTests()
        {
            _mockLogger = new Mock<ILogger<ReportingService>>();
            _reportingService = new ReportingService(_mockLogger.Object);
        }

        /// <summary>
        /// Valida que el método GenerateMorosidadPdfAsync genera un archivo PDF
        /// correctamente cuando se le provee una lista de datos de morosidad.
        /// La prueba se enfoca en verificar que el resultado no es nulo y que
        /// el archivo generado contiene los bytes esperados de un PDF (magic number %PDF-).
        /// </summary>
        /*
        [Fact]
        public async Task GenerateMorosidadPdfAsync_DebeGenerarPdf_ConDatosValidos()
        {
            // Arrange
            var morosidadData = new List<MorosidadInfo>
            {
                new MorosidadInfo { No = 1, Banco = "Banco A", Retailer = "RTL001", Nombre = "Comercio Uno", Monto = 100, Saldo = 50, Pte = 1, Inicio = System.DateTime.Now, Mes = "06/2025", Pos = 1, Estado = "Activo", Abonos = 0, DebA = 0, DebC = 0, Max = 0}
            };

            // Act
            var pdfBytes = await _reportingService.GenerateMorosidadPdfAsync(morosidadData, null);

            // Assert
            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 0);
            
            // Verificar el "magic number" de PDF (%PDF-) para confirmar que es un PDF.
            // Esto es más robusto que solo chequear que el array de bytes no está vacío.
            var pdfSignature = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // %PDF-
            var actualSignature = pdfBytes.Take(pdfSignature.Length).ToArray();
            Assert.Equal(pdfSignature, actualSignature);
        }
        */

        /// <summary>
        /// Valida que el método de generación de PDF no falle y produzca un
        /// documento válido incluso cuando no hay datos de morosidad que procesar.
        /// </summary>
        [Fact]
        public async Task GenerateMorosidadPdfAsync_DebeGenerarPdfVacio_CuandoNoHayDatos()
        {
            // Arrange
            var morosidadData = new List<MorosidadInfo>();

            // Act
            var pdfBytes = await _reportingService.GenerateMorosidadPdfAsync(morosidadData, null);

            // Assert
            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 0);

            var pdfSignature = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // %PDF-
            var actualSignature = pdfBytes.Take(pdfSignature.Length).ToArray();
            Assert.Equal(pdfSignature, actualSignature);
        }

        /// <summary>
        /// Valida que el método GenerateMorosidadPdfAsync incluye correctamente el logo
        /// cuando se proporciona uno.
        /// </summary>
        [Fact]
        public async Task GenerateMorosidadPdfAsync_DebeIncluirLogo_CuandoSeProporcionaLogo()
        {
            // Arrange
            var morosidadData = new List<MorosidadInfo>
            {
                new MorosidadInfo { No = 1, Banco = "Banco A", Retailer = "RTL001", Nombre = "Comercio Uno", Monto = 100, Saldo = 50, Pte = 1, Inicio = System.DateTime.Now, Mes = "06/2025", Pos = 1, Estado = "Activo", Abonos = 0, DebA = 0, DebC = 0, Max = 0}
            };
            var logoBytes = new byte[] { 1, 2, 3, 4, 5 }; // Logo simulado

            // Act
            var pdfBytes = await _reportingService.GenerateMorosidadPdfAsync(morosidadData, logoBytes);

            // Assert
            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 0);
            
            var pdfSignature = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // %PDF-
            var actualSignature = pdfBytes.Take(pdfSignature.Length).ToArray();
            Assert.Equal(pdfSignature, actualSignature);
        }

        /// <summary>
        /// Valida que el método GenerateMorosidadExcelAsync maneja correctamente
        /// una lista con pocos datos de morosidad.
        /// </summary>
        [Fact]
        public async Task GenerateMorosidadExcelAsync_DebeGenerarExcelMinimo_ConPocosDatos()
        {
            // Arrange - Solo un registro para probar el caso mínimo
            var morosidadData = new List<MorosidadInfo>
            {
                new MorosidadInfo { No = 1, Banco = "Banco Test", Retailer = "RTL001", Nombre = "Test", Monto = 100, Saldo = 50, Pte = 1, Inicio = System.DateTime.Now, Mes = "06/2025", Pos = 1, Estado = "Activo", Abonos = 0, DebA = 0, DebC = 0, Max = 0}
            };

            // Act
            var excelBytes = await _reportingService.GenerateMorosidadExcelAsync(morosidadData);

            // Assert
            Assert.NotNull(excelBytes);
            Assert.True(excelBytes.Length > 0);

            var zipSignature = new byte[] { 0x50, 0x4B }; // PK (ZIP header)
            var actualSignature = excelBytes.Take(zipSignature.Length).ToArray();
            Assert.Equal(zipSignature, actualSignature);
        }

        /// <summary>
        /// Valida que el método GenerateInactivosExcelAsync genera un archivo Excel
        /// válido con datos de comercios inactivos.
        /// </summary>
        [Fact]
        public async Task GenerateInactivosExcelAsync_DebeGenerarExcel_ConDatosValidos()
        {
            // Arrange
            var inactivosData = new List<InactivoInfo>
            {
                new InactivoInfo { No = 1, Banco = "Banco A", Retailer = "RTL001", Nombre = "Comercio Inactivo Uno", Estado = "Inactivo", Retiro = System.DateTime.Now.AddDays(-90), Inicio = System.DateTime.Now.AddDays(-180) },
                new InactivoInfo { No = 2, Banco = "Banco B", Retailer = "RTL002", Nombre = "Comercio Inactivo Dos", Estado = "Suspendido", Retiro = System.DateTime.Now.AddDays(-120), Inicio = System.DateTime.Now.AddDays(-200) }
            };

            // Act
            var excelBytes = await _reportingService.GenerateInactivosExcelAsync(inactivosData);

            // Assert
            Assert.NotNull(excelBytes);
            Assert.True(excelBytes.Length > 0);
            
            // Verificar el "magic number" de Excel (ZIP signature para XLSX)
            var zipSignature = new byte[] { 0x50, 0x4B }; // PK (ZIP header)
            var actualSignature = excelBytes.Take(zipSignature.Length).ToArray();
            Assert.Equal(zipSignature, actualSignature);
        }

        /// <summary>
        /// Valida que el método GenerateInactivosExcelAsync maneja correctamente
        /// una lista con un solo dato de inactivos.
        /// </summary>
        [Fact]
        public async Task GenerateInactivosExcelAsync_DebeGenerarExcelMinimo_ConUnSoloRegistro()
        {
            // Arrange
            var inactivosData = new List<InactivoInfo>
            {
                new InactivoInfo { No = 1, Banco = "Banco Test", Retailer = "RTL001", Nombre = "Test Inactivo", Estado = "Inactivo", Retiro = System.DateTime.Now.AddDays(-90), Inicio = System.DateTime.Now.AddDays(-180) }
            };

            // Act
            var excelBytes = await _reportingService.GenerateInactivosExcelAsync(inactivosData);

            // Assert
            Assert.NotNull(excelBytes);
            Assert.True(excelBytes.Length > 0);

            var zipSignature = new byte[] { 0x50, 0x4B }; // PK (ZIP header)
            var actualSignature = excelBytes.Take(zipSignature.Length).ToArray();
            Assert.Equal(zipSignature, actualSignature);
        }




    }
} 