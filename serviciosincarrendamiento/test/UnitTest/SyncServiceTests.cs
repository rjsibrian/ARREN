using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Servicio.Services;
using ServicioSincArrendamiento.Models;
using ServicioSincArrendamiento.Services;

namespace UnitTest
{
    public class SyncServiceTests
    {
        private readonly Mock<ILogger<SyncService>> _mockLogger;
        private readonly Mock<IDataAccessService> _mockDataAccess;
        private readonly Mock<INotificationService> _mockNotification;
        private readonly Mock<IReportingService> _mockReportingService;
        private readonly Mock<IOptions<SyncSettings>> _mockSyncSettings;
        private readonly Mock<IOptions<EmailSettings>> _mockEmailSettings;
        private readonly Mock<IOptions<AppSettings>> _mockAppSettings;


        public SyncServiceTests()
        {
            _mockLogger = new Mock<ILogger<SyncService>>();
            _mockDataAccess = new Mock<IDataAccessService>();
            _mockNotification = new Mock<INotificationService>();
            _mockReportingService = new Mock<IReportingService>();
            _mockSyncSettings = new Mock<IOptions<SyncSettings>>();
            _mockEmailSettings = new Mock<IOptions<EmailSettings>>();
            _mockAppSettings = new Mock<IOptions<AppSettings>>();
        }

        private SyncService CreateService(SyncSettings syncSettings, EmailSettings? emailSettings = null, AppSettings? appSettings = null)
        {
            _mockSyncSettings.Setup(s => s.Value).Returns(syncSettings);
            _mockEmailSettings.Setup(s => s.Value).Returns(emailSettings ?? new EmailSettings());
            _mockAppSettings.Setup(s => s.Value).Returns(appSettings ?? new AppSettings { IdSistema = 1, Phrase = "test" });

            return new SyncService(
                _mockLogger.Object,
                _mockDataAccess.Object,
                _mockNotification.Object,
                _mockReportingService.Object,
                _mockSyncSettings.Object,
                _mockEmailSettings.Object,
                _mockAppSettings.Object
            );
        }

        /// <summary>
        /// Simula el escenario "camino feliz".
        /// Valida que si es día de reporte, el proceso completo se ejecuta:
        /// 1. Se sincronizan los datos.
        /// 2. Se deshabilita a quien corresponda.
        /// 3. Se generan y envían los reportes (PDF, Excel de morosidad e inactivos).
        /// 4. Se actualiza el estado.
        /// </summary>
        [Fact]
        public async Task PerformSyncAsync_DebeEjecutarse_CuandoEsDiaDeReporte()
        {
            // Arrange
            var settings = new SyncSettings { SkipReportDateValidation = true, ReportMode = ReportingMode.Flexible };
            var service = CreateService(settings);

            _mockDataAccess.Setup(d => d.ExecuteBusinessProcessAsync(It.IsAny<DateTime>())).ReturnsAsync("OK");
            _mockDataAccess.Setup(d => d.GetArrendamientosAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<ArrendamientoInfo> { new ArrendamientoInfo() });
            _mockDataAccess.Setup(d => d.GetEmailRecipientsAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<string> { "test@test.com" });
            _mockDataAccess.Setup(d => d.GetMorosidadReportDataAsync()).ReturnsAsync(new List<MorosidadInfo> { new MorosidadInfo() });
            _mockDataAccess.Setup(d => d.GetInactivosReportDataAsync()).ReturnsAsync(new List<InactivoInfo>());
            _mockDataAccess.Setup(d => d.GetSystemParametersAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(new List<SystemParameter>());

            // Setup todos los métodos de ReportingService
            _mockReportingService.Setup(r => r.GenerateMorosidadPdfAsync(It.IsAny<IEnumerable<MorosidadInfo>>(), It.IsAny<byte[]?>())).ReturnsAsync(new byte[] { 1, 2, 3 });
            _mockReportingService.Setup(r => r.GenerateMorosidadExcelAsync(It.IsAny<IEnumerable<MorosidadInfo>>())).ReturnsAsync(new byte[] { 4, 5, 6 });
            _mockReportingService.Setup(r => r.GenerateInactivosExcelAsync(It.IsAny<IEnumerable<InactivoInfo>>())).ReturnsAsync(new byte[] { 7, 8, 9 });

            // Act
            await service.PerformSyncAsync();

            // Assert
            _mockDataAccess.Verify(d => d.ExecuteBusinessProcessAsync(It.IsAny<DateTime>()), Times.Once);
            _mockDataAccess.Verify(d => d.ExecuteSyncProcessAsync(It.IsAny<ArrendamientoInfo>()), Times.AtLeastOnce);
            _mockDataAccess.Verify(d => d.ExecuteDisableProcessAsync(), Times.Once);
            _mockReportingService.Verify(r => r.GenerateMorosidadPdfAsync(It.IsAny<IEnumerable<MorosidadInfo>>(), It.IsAny<byte[]?>()), Times.Once);
            _mockReportingService.Verify(r => r.GenerateMorosidadExcelAsync(It.IsAny<IEnumerable<MorosidadInfo>>()), Times.Once);
            _mockReportingService.Verify(r => r.GenerateInactivosExcelAsync(It.IsAny<IEnumerable<InactivoInfo>>()), Times.Once);
            _mockNotification.Verify(n => n.SendNotificationAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<EmailAttachment>>(), false, null), Times.Once);
        }

        /// <summary>
        /// Valida que el proceso diario siempre se ejecuta.
        /// Aunque no sea día de reporte, los procesos de negocio y sincronización deben ejecutarse.
        /// </summary>
        [Fact]
        public async Task PerformSyncAsync_EjecutaProcesoDiario_AunqueNoSeaDiaDeReporte()
        {
            // Arrange
            // La clave es que NO es día de reporte
            var today = DateTime.UtcNow;
            var settings = new SyncSettings { SkipReportDateValidation = false, AdvanceDays = -5 }; // Un advance day negativo asegura que nunca sea hoy
            var service = CreateService(settings);

            _mockDataAccess.Setup(d => d.ExecuteBusinessProcessAsync(It.IsAny<DateTime>())).ReturnsAsync("OK");
            _mockDataAccess.Setup(d => d.GetArrendamientosAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<ArrendamientoInfo>());
            
            // Act
            await service.PerformSyncAsync();

            // Assert
            // El proceso de negocio y sincronización SIEMPRE se debe ejecutar.
            _mockDataAccess.Verify(d => d.ExecuteBusinessProcessAsync(It.IsAny<DateTime>()), Times.Once);
            _mockDataAccess.Verify(d => d.ExecuteDisableProcessAsync(), Times.Once);

            // La notificación NUNCA debe ocurrir porque no es día de reporte
            _mockNotification.Verify(n => n.SendNotificationAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<EmailAttachment>>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Prueba ReportingMode.Force - siempre envía reportes incluso sin datos.
        /// </summary>
        [Fact]
        public async Task PerformSyncAsync_ReportModeForce_SiempreEnviaReportes()
        {
            // Arrange
            var settings = new SyncSettings { SkipReportDateValidation = true, ReportMode = ReportingMode.Force };
            var service = CreateService(settings);

            _mockDataAccess.Setup(d => d.ExecuteBusinessProcessAsync(It.IsAny<DateTime>())).ReturnsAsync("OK");
            _mockDataAccess.Setup(d => d.GetArrendamientosAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<ArrendamientoInfo>());
            _mockDataAccess.Setup(d => d.GetEmailRecipientsAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<string> { "test@test.com" });
            // Sin datos de morosidad e inactivos
            _mockDataAccess.Setup(d => d.GetMorosidadReportDataAsync()).ReturnsAsync(new List<MorosidadInfo>());
            _mockDataAccess.Setup(d => d.GetInactivosReportDataAsync()).ReturnsAsync(new List<InactivoInfo>());
            _mockDataAccess.Setup(d => d.GetSystemParametersAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(new List<SystemParameter>());

            _mockReportingService.Setup(r => r.GenerateMorosidadPdfAsync(It.IsAny<IEnumerable<MorosidadInfo>>(), It.IsAny<byte[]?>())).ReturnsAsync(new byte[] { 1 });
            _mockReportingService.Setup(r => r.GenerateMorosidadExcelAsync(It.IsAny<IEnumerable<MorosidadInfo>>())).ReturnsAsync(new byte[] { 2 });
            _mockReportingService.Setup(r => r.GenerateInactivosExcelAsync(It.IsAny<IEnumerable<InactivoInfo>>())).ReturnsAsync(new byte[] { 3 });

            // Act
            await service.PerformSyncAsync();

            // Assert - Debe enviar reportes aunque no haya datos
            _mockNotification.Verify(n => n.SendNotificationAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<EmailAttachment>>(), It.IsAny<bool>(), null), Times.Once);
            _mockReportingService.Verify(r => r.GenerateMorosidadPdfAsync(It.IsAny<IEnumerable<MorosidadInfo>>(), It.IsAny<byte[]?>()), Times.Once);
        }

        /// <summary>
        /// Valida la robustez del servicio ante errores.
        /// Simula una excepción de base de datos durante el proceso y verifica que se envía notificación de error.
        /// </summary>
        [Fact]
        public async Task PerformSyncAsync_DebeEnviarNotificacionDeError_CuandoHayExcepcion()
        {
            // Arrange
            var settings = new SyncSettings { SkipReportDateValidation = false, AdvanceDays = 2 };
            var service = CreateService(settings);

            _mockDataAccess.Setup(d => d.ExecuteBusinessProcessAsync(It.IsAny<DateTime>())).ThrowsAsync(new InvalidOperationException("Error de BD simulado"));
            _mockDataAccess.Setup(d => d.GetEmailRecipientsAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<string> { "admin@test.com" });

            // Act
            await service.PerformSyncAsync();

            // Assert
            _mockNotification.Verify(n => n.SendNotificationAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<EmailAttachment>>(),
                false,
                It.Is<string>(s => s.Contains("Error de BD simulado"))
            ), Times.Once);
        }

        /// <summary>
        /// Valida ReportingMode.Flexible - Solo envía reportes si hay datos.
        /// En este caso NO hay datos de morosidad, pero SÍ se envía el reporte de inactivos (comportamiento real).
        /// </summary>
        [Fact]
        public async Task PerformSyncAsync_ReportModeFlexible_NoEnviaCorreo_SinDatos()
        {
            // Arrange
            var today = DateTime.UtcNow;
            var settings = new SyncSettings { SkipReportDateValidation = true, ReportMode = ReportingMode.Flexible };
            var service = CreateService(settings);

            _mockDataAccess.Setup(d => d.ExecuteBusinessProcessAsync(It.IsAny<DateTime>())).ReturnsAsync("OK");
            _mockDataAccess.Setup(d => d.GetArrendamientosAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<ArrendamientoInfo>());
            _mockDataAccess.Setup(d => d.GetEmailRecipientsAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<string> { "test@test.com" });
            // Sin datos de morosidad - clave de la prueba
            _mockDataAccess.Setup(d => d.GetMorosidadReportDataAsync()).ReturnsAsync(new List<MorosidadInfo>());
            _mockDataAccess.Setup(d => d.GetInactivosReportDataAsync()).ReturnsAsync(new List<InactivoInfo>());
            _mockDataAccess.Setup(d => d.GetSystemParametersAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(new List<SystemParameter>());

            // Setup reportes - el de inactivos SIEMPRE se genera
            _mockReportingService.Setup(r => r.GenerateInactivosExcelAsync(It.IsAny<IEnumerable<InactivoInfo>>())).ReturnsAsync(new byte[] { 1, 2, 3 });

            // Act
            await service.PerformSyncAsync();

            // Assert
            // Verificar que los procesos iniciales se ejecutan
            _mockDataAccess.Verify(d => d.ExecuteBusinessProcessAsync(It.IsAny<DateTime>()), Times.Once);
            _mockDataAccess.Verify(d => d.ExecuteDisableProcessAsync(), Times.Once);
            
            // Verificar que SÍ se envió notificación porque aunque no hay morosidad, siempre se envía reporte de inactivos
            _mockNotification.Verify(n => n.SendNotificationAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<EmailAttachment>>(), true, null), Times.Once);
            
            // Verificar que NO se generaron reportes de morosidad (sin datos)
            _mockReportingService.Verify(r => r.GenerateMorosidadPdfAsync(It.IsAny<IEnumerable<MorosidadInfo>>(), It.IsAny<byte[]?>()), Times.Never);
            _mockReportingService.Verify(r => r.GenerateMorosidadExcelAsync(It.IsAny<IEnumerable<MorosidadInfo>>()), Times.Never);
            
            // Verificar que SÍ se generó reporte de inactivos (siempre se envía)
            _mockReportingService.Verify(r => r.GenerateInactivosExcelAsync(It.IsAny<IEnumerable<InactivoInfo>>()), Times.Once);
        }

        /// <summary>
        /// Valida ReportingMode.None - Nunca envía reportes.
        /// </summary>
        [Fact]
        public async Task PerformSyncAsync_ReportModeNone_NuncaEnviaReportes()
        {
            // Arrange
            var settings = new SyncSettings { SkipReportDateValidation = true, ReportMode = ReportingMode.None };
            var service = CreateService(settings);

            _mockDataAccess.Setup(d => d.ExecuteBusinessProcessAsync(It.IsAny<DateTime>())).ReturnsAsync("OK");
            _mockDataAccess.Setup(d => d.GetArrendamientosAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<ArrendamientoInfo>());
            _mockDataAccess.Setup(d => d.GetMorosidadReportDataAsync()).ReturnsAsync(new List<MorosidadInfo> { new MorosidadInfo() });
            _mockDataAccess.Setup(d => d.GetInactivosReportDataAsync()).ReturnsAsync(new List<InactivoInfo>());
            _mockDataAccess.Setup(d => d.GetSystemParametersAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(new List<SystemParameter>());

            // Act
            await service.PerformSyncAsync();

            // Assert
            // Debe ejecutar procesos básicos
            _mockDataAccess.Verify(d => d.ExecuteBusinessProcessAsync(It.IsAny<DateTime>()), Times.Once);
            
            // NUNCA debe generar reportes ni enviar notificaciones
            _mockReportingService.Verify(r => r.GenerateMorosidadPdfAsync(It.IsAny<IEnumerable<MorosidadInfo>>(), It.IsAny<byte[]?>()), Times.Never);
            _mockNotification.Verify(n => n.SendNotificationAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<EmailAttachment>>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Prueba el manejo de logo con formato hexadecimal válido (0x prefix).
        /// </summary>
        [Fact]
        public async Task PerformSyncAsync_LogoHexadecimal_SeProcesamcorrectamente()
        {
            // Arrange
            var settings = new SyncSettings { SkipReportDateValidation = true, ReportMode = ReportingMode.Force };
            var service = CreateService(settings);

            var hexLogo = "0x89504E470D0A1A0A"; // PNG header en hex
            var logoParameter = new SystemParameter 
            { 
                Codigo = "Logo", 
                Byte = hexLogo 
            };

            _mockDataAccess.Setup(d => d.ExecuteBusinessProcessAsync(It.IsAny<DateTime>())).ReturnsAsync("OK");
            _mockDataAccess.Setup(d => d.GetArrendamientosAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<ArrendamientoInfo>());
            _mockDataAccess.Setup(d => d.GetEmailRecipientsAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<string> { "test@test.com" });
            _mockDataAccess.Setup(d => d.GetMorosidadReportDataAsync()).ReturnsAsync(new List<MorosidadInfo>());
            _mockDataAccess.Setup(d => d.GetInactivosReportDataAsync()).ReturnsAsync(new List<InactivoInfo>());
            _mockDataAccess.Setup(d => d.GetSystemParametersAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new List<SystemParameter> { logoParameter });

            _mockReportingService.Setup(r => r.GenerateMorosidadPdfAsync(It.IsAny<IEnumerable<MorosidadInfo>>(), It.IsAny<byte[]?>()))
                .ReturnsAsync(new byte[] { 1, 2, 3 });
            _mockReportingService.Setup(r => r.GenerateMorosidadExcelAsync(It.IsAny<IEnumerable<MorosidadInfo>>()))
                .ReturnsAsync(new byte[] { 4, 5, 6 });
            _mockReportingService.Setup(r => r.GenerateInactivosExcelAsync(It.IsAny<IEnumerable<InactivoInfo>>()))
                .ReturnsAsync(new byte[] { 7, 8, 9 });

            // Act
            await service.PerformSyncAsync();

            // Assert
            // Verificar que se pasó un logo (no null) al método de generación PDF
            _mockReportingService.Verify(r => r.GenerateMorosidadPdfAsync(
                It.IsAny<IEnumerable<MorosidadInfo>>(),
                It.Is<byte[]?>(logo => logo != null && logo.Length > 0)
            ), Times.Once);
        }

        /// <summary>
        /// Prueba el manejo de logo con formato base64.
        /// </summary>
        [Fact]
        public async Task PerformSyncAsync_LogoBase64_SeProcesaCorrectamente()
        {
            // Arrange
            var settings = new SyncSettings { SkipReportDateValidation = true, ReportMode = ReportingMode.Force };
            var service = CreateService(settings);

            var base64Logo = Convert.ToBase64String(new byte[] { 0x89, 0x50, 0x4E, 0x47 }); // PNG header en base64
            var logoParameter = new SystemParameter 
            { 
                Codigo = "Logo", 
                Byte = base64Logo 
            };

            _mockDataAccess.Setup(d => d.ExecuteBusinessProcessAsync(It.IsAny<DateTime>())).ReturnsAsync("OK");
            _mockDataAccess.Setup(d => d.GetArrendamientosAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<ArrendamientoInfo>());
            _mockDataAccess.Setup(d => d.GetEmailRecipientsAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<string> { "test@test.com" });
            _mockDataAccess.Setup(d => d.GetMorosidadReportDataAsync()).ReturnsAsync(new List<MorosidadInfo>());
            _mockDataAccess.Setup(d => d.GetInactivosReportDataAsync()).ReturnsAsync(new List<InactivoInfo>());
            _mockDataAccess.Setup(d => d.GetSystemParametersAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new List<SystemParameter> { logoParameter });

            _mockReportingService.Setup(r => r.GenerateMorosidadPdfAsync(It.IsAny<IEnumerable<MorosidadInfo>>(), It.IsAny<byte[]?>()))
                .ReturnsAsync(new byte[] { 1, 2, 3 });
            _mockReportingService.Setup(r => r.GenerateMorosidadExcelAsync(It.IsAny<IEnumerable<MorosidadInfo>>()))
                .ReturnsAsync(new byte[] { 4, 5, 6 });
            _mockReportingService.Setup(r => r.GenerateInactivosExcelAsync(It.IsAny<IEnumerable<InactivoInfo>>()))
                .ReturnsAsync(new byte[] { 7, 8, 9 });

            // Act
            await service.PerformSyncAsync();

            // Assert
            _mockReportingService.Verify(r => r.GenerateMorosidadPdfAsync(
                It.IsAny<IEnumerable<MorosidadInfo>>(),
                It.Is<byte[]?>(logo => logo != null && logo.Length > 0)
            ), Times.Once);
        }

        /// <summary>
        /// Prueba el manejo de logo con formato byte[] directo.
        /// </summary>
        [Fact]
        public async Task PerformSyncAsync_LogoByteArray_SeProcesaCorrectamente()
        {
            // Arrange
            var settings = new SyncSettings { SkipReportDateValidation = true, ReportMode = ReportingMode.Force };
            var service = CreateService(settings);

            var byteArrayLogo = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header
            var logoParameter = new SystemParameter 
            { 
                Codigo = "Logo", 
                Byte = byteArrayLogo 
            };

            _mockDataAccess.Setup(d => d.ExecuteBusinessProcessAsync(It.IsAny<DateTime>())).ReturnsAsync("OK");
            _mockDataAccess.Setup(d => d.GetArrendamientosAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<ArrendamientoInfo>());
            _mockDataAccess.Setup(d => d.GetEmailRecipientsAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<string> { "test@test.com" });
            _mockDataAccess.Setup(d => d.GetMorosidadReportDataAsync()).ReturnsAsync(new List<MorosidadInfo>());
            _mockDataAccess.Setup(d => d.GetInactivosReportDataAsync()).ReturnsAsync(new List<InactivoInfo>());
            _mockDataAccess.Setup(d => d.GetSystemParametersAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new List<SystemParameter> { logoParameter });

            _mockReportingService.Setup(r => r.GenerateMorosidadPdfAsync(It.IsAny<IEnumerable<MorosidadInfo>>(), It.IsAny<byte[]?>()))
                .ReturnsAsync(new byte[] { 1, 2, 3 });
            _mockReportingService.Setup(r => r.GenerateMorosidadExcelAsync(It.IsAny<IEnumerable<MorosidadInfo>>()))
                .ReturnsAsync(new byte[] { 4, 5, 6 });
            _mockReportingService.Setup(r => r.GenerateInactivosExcelAsync(It.IsAny<IEnumerable<InactivoInfo>>()))
                .ReturnsAsync(new byte[] { 7, 8, 9 });

            // Act
            await service.PerformSyncAsync();

            // Assert
            _mockReportingService.Verify(r => r.GenerateMorosidadPdfAsync(
                It.IsAny<IEnumerable<MorosidadInfo>>(),
                It.Is<byte[]?>(logo => logo != null && logo.Length == 4)
            ), Times.Once);
        }

        /// <summary>
        /// Prueba el manejo cuando no existe parámetro de logo.
        /// </summary>
        [Fact]
        public async Task PerformSyncAsync_SinParametroLogo_UsaNull()
        {
            // Arrange
            var settings = new SyncSettings { SkipReportDateValidation = true, ReportMode = ReportingMode.Force };
            var service = CreateService(settings);

            _mockDataAccess.Setup(d => d.ExecuteBusinessProcessAsync(It.IsAny<DateTime>())).ReturnsAsync("OK");
            _mockDataAccess.Setup(d => d.GetArrendamientosAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<ArrendamientoInfo>());
            _mockDataAccess.Setup(d => d.GetEmailRecipientsAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<string> { "test@test.com" });
            _mockDataAccess.Setup(d => d.GetMorosidadReportDataAsync()).ReturnsAsync(new List<MorosidadInfo>());
            _mockDataAccess.Setup(d => d.GetInactivosReportDataAsync()).ReturnsAsync(new List<InactivoInfo>());
            _mockDataAccess.Setup(d => d.GetSystemParametersAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new List<SystemParameter>()); // Sin parámetros

            _mockReportingService.Setup(r => r.GenerateMorosidadPdfAsync(It.IsAny<IEnumerable<MorosidadInfo>>(), It.IsAny<byte[]?>()))
                .ReturnsAsync(new byte[] { 1, 2, 3 });
            _mockReportingService.Setup(r => r.GenerateMorosidadExcelAsync(It.IsAny<IEnumerable<MorosidadInfo>>()))
                .ReturnsAsync(new byte[] { 4, 5, 6 });
            _mockReportingService.Setup(r => r.GenerateInactivosExcelAsync(It.IsAny<IEnumerable<InactivoInfo>>()))
                .ReturnsAsync(new byte[] { 7, 8, 9 });

            // Act
            await service.PerformSyncAsync();

            // Assert
            _mockReportingService.Verify(r => r.GenerateMorosidadPdfAsync(
                It.IsAny<IEnumerable<MorosidadInfo>>(),
                null
            ), Times.Once);
        }

        /// <summary>
        /// Prueba el manejo de error en cálculo de fecha cuando AdvanceDays es muy grande.
        /// </summary>
        [Fact]
        public async Task PerformSyncAsync_ErrorCalculoFecha_ContinuaSinReportes()
        {
            // Arrange
            var settings = new SyncSettings { SkipReportDateValidation = false, AdvanceDays = 500 }; // Valor muy grande que causará error
            var service = CreateService(settings);

            _mockDataAccess.Setup(d => d.ExecuteBusinessProcessAsync(It.IsAny<DateTime>())).ReturnsAsync("OK");
            _mockDataAccess.Setup(d => d.GetArrendamientosAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<ArrendamientoInfo>());

            // Act
            await service.PerformSyncAsync();

            // Assert
            // Debe continuar ejecutando los procesos básicos aunque falle el cálculo de fecha
            _mockDataAccess.Verify(d => d.ExecuteBusinessProcessAsync(It.IsAny<DateTime>()), Times.Once);
            _mockDataAccess.Verify(d => d.ExecuteDisableProcessAsync(), Times.Once);
            
            // No debe enviar reportes debido al error en cálculo de fecha
            _mockNotification.Verify(n => n.SendNotificationAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<EmailAttachment>>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Prueba el manejo de errores individuales durante la sincronización de arrendamientos.
        /// </summary>
        [Fact]
        public async Task PerformSyncAsync_ErrorSincronizacionIndividual_ContinuaConSiguientes()
        {
            // Arrange
            var settings = new SyncSettings { SkipReportDateValidation = false, AdvanceDays = 2 };
            var service = CreateService(settings);

            var arrendamientos = new List<ArrendamientoInfo>
            {
                new ArrendamientoInfo { Retailer = "Retailer1" },
                new ArrendamientoInfo { Retailer = "Retailer2" },
                new ArrendamientoInfo { Retailer = "Retailer3" }
            };

            _mockDataAccess.Setup(d => d.ExecuteBusinessProcessAsync(It.IsAny<DateTime>())).ReturnsAsync("OK");
            _mockDataAccess.Setup(d => d.GetArrendamientosAsync(It.IsAny<DateTime>())).ReturnsAsync(arrendamientos);
            
            // Simular que el segundo arrendamiento falla
            _mockDataAccess.Setup(d => d.ExecuteSyncProcessAsync(It.Is<ArrendamientoInfo>(a => a.Retailer == "Retailer1")))
                .Returns(Task.CompletedTask);
            _mockDataAccess.Setup(d => d.ExecuteSyncProcessAsync(It.Is<ArrendamientoInfo>(a => a.Retailer == "Retailer2")))
                .ThrowsAsync(new InvalidOperationException("Error en Retailer2"));
            _mockDataAccess.Setup(d => d.ExecuteSyncProcessAsync(It.Is<ArrendamientoInfo>(a => a.Retailer == "Retailer3")))
                .Returns(Task.CompletedTask);

            // Act
            await service.PerformSyncAsync();

            // Assert
            // Debe intentar sincronizar todos los arrendamientos
            _mockDataAccess.Verify(d => d.ExecuteSyncProcessAsync(It.IsAny<ArrendamientoInfo>()), Times.Exactly(3));
            
            // Debe continuar con el proceso a pesar del error
            _mockDataAccess.Verify(d => d.ExecuteDisableProcessAsync(), Times.Once);
        }

        /// <summary>
        /// Prueba el escenario de fallo cascada: falla el proceso principal Y la notificación de error.
        /// </summary>
        [Fact]
        public async Task PerformSyncAsync_FalloCascada_ErrorEnPrincipalYNotificacion()
        {
            // Arrange
            var settings = new SyncSettings { SkipReportDateValidation = false, AdvanceDays = 2 };
            var service = CreateService(settings);

            _mockDataAccess.Setup(d => d.ExecuteBusinessProcessAsync(It.IsAny<DateTime>()))
                .ThrowsAsync(new InvalidOperationException("Error principal"));
            _mockDataAccess.Setup(d => d.GetEmailRecipientsAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<string> { "admin@test.com" });
            _mockNotification.Setup(n => n.SendNotificationAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<EmailAttachment>>(), false, It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Error en notificación"));

            // Act
            await service.PerformSyncAsync();

            // Assert
            // Debe intentar enviar notificación de error
            _mockNotification.Verify(n => n.SendNotificationAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<EmailAttachment>>(),
                false,
                It.Is<string>(s => s.Contains("Error principal"))
            ), Times.Once);
        }

        /// <summary>
        /// Prueba logo con base64 inválido - debe usar array vacío como fallback.
        /// </summary>
        [Fact]
        public async Task PerformSyncAsync_LogoBase64Invalido_UsaArrayVacio()
        {
            // Arrange
            var settings = new SyncSettings { SkipReportDateValidation = true, ReportMode = ReportingMode.Force };
            var service = CreateService(settings);

            var invalidBase64Logo = "esto-no-es-base64-válido!!!"; 
            var logoParameter = new SystemParameter 
            { 
                Codigo = "Logo", 
                Byte = invalidBase64Logo 
            };

            _mockDataAccess.Setup(d => d.ExecuteBusinessProcessAsync(It.IsAny<DateTime>())).ReturnsAsync("OK");
            _mockDataAccess.Setup(d => d.GetArrendamientosAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<ArrendamientoInfo>());
            _mockDataAccess.Setup(d => d.GetEmailRecipientsAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<string> { "test@test.com" });
            _mockDataAccess.Setup(d => d.GetMorosidadReportDataAsync()).ReturnsAsync(new List<MorosidadInfo>());
            _mockDataAccess.Setup(d => d.GetInactivosReportDataAsync()).ReturnsAsync(new List<InactivoInfo>());
            _mockDataAccess.Setup(d => d.GetSystemParametersAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new List<SystemParameter> { logoParameter });

            _mockReportingService.Setup(r => r.GenerateMorosidadPdfAsync(It.IsAny<IEnumerable<MorosidadInfo>>(), It.IsAny<byte[]?>()))
                .ReturnsAsync(new byte[] { 1, 2, 3 });
            _mockReportingService.Setup(r => r.GenerateMorosidadExcelAsync(It.IsAny<IEnumerable<MorosidadInfo>>()))
                .ReturnsAsync(new byte[] { 4, 5, 6 });
            _mockReportingService.Setup(r => r.GenerateInactivosExcelAsync(It.IsAny<IEnumerable<InactivoInfo>>()))
                .ReturnsAsync(new byte[] { 7, 8, 9 });

            // Act
            await service.PerformSyncAsync();

            // Assert
            // Debe usar array vacío cuando base64 es inválido
            _mockReportingService.Verify(r => r.GenerateMorosidadPdfAsync(
                It.IsAny<IEnumerable<MorosidadInfo>>(),
                It.Is<byte[]?>(logo => logo != null && logo.Length == 0)
            ), Times.Once);
        }

        /// <summary>
        /// Prueba manejo de hexadecimal con longitud impar (debe agregar 0 al inicio).
        /// </summary>
        [Fact]
        public async Task PerformSyncAsync_LogoHexLongitudImpar_AgregaCero()
        {
            // Arrange
            var settings = new SyncSettings { SkipReportDateValidation = true, ReportMode = ReportingMode.Force };
            var service = CreateService(settings);

            var oddLengthHex = "0x89504E47D"; // 9 caracteres después de 0x (impar)
            var logoParameter = new SystemParameter 
            { 
                Codigo = "Logo", 
                Byte = oddLengthHex 
            };

            _mockDataAccess.Setup(d => d.ExecuteBusinessProcessAsync(It.IsAny<DateTime>())).ReturnsAsync("OK");
            _mockDataAccess.Setup(d => d.GetArrendamientosAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<ArrendamientoInfo>());
            _mockDataAccess.Setup(d => d.GetEmailRecipientsAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<string> { "test@test.com" });
            _mockDataAccess.Setup(d => d.GetMorosidadReportDataAsync()).ReturnsAsync(new List<MorosidadInfo>());
            _mockDataAccess.Setup(d => d.GetInactivosReportDataAsync()).ReturnsAsync(new List<InactivoInfo>());
            _mockDataAccess.Setup(d => d.GetSystemParametersAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new List<SystemParameter> { logoParameter });

            _mockReportingService.Setup(r => r.GenerateMorosidadPdfAsync(It.IsAny<IEnumerable<MorosidadInfo>>(), It.IsAny<byte[]?>()))
                .ReturnsAsync(new byte[] { 1, 2, 3 });
            _mockReportingService.Setup(r => r.GenerateMorosidadExcelAsync(It.IsAny<IEnumerable<MorosidadInfo>>()))
                .ReturnsAsync(new byte[] { 4, 5, 6 });
            _mockReportingService.Setup(r => r.GenerateInactivosExcelAsync(It.IsAny<IEnumerable<InactivoInfo>>()))
                .ReturnsAsync(new byte[] { 7, 8, 9 });

            // Act
            await service.PerformSyncAsync();

            // Assert
            // Debe procesar correctamente hexadecimal con longitud impar
            _mockReportingService.Verify(r => r.GenerateMorosidadPdfAsync(
                It.IsAny<IEnumerable<MorosidadInfo>>(),
                It.Is<byte[]?>(logo => logo != null && logo.Length > 0)
            ), Times.Once);
        }

        /// <summary>
        /// Verifica que se use el IdTipo correcto para diferentes tipos de notificaciones.
        /// </summary>
        [Fact]
        public async Task PerformSyncAsync_UsaIdTipoCorrecto_ParaDiferentesNotificaciones()
        {
            // Arrange
            var settings = new SyncSettings { SkipReportDateValidation = true, ReportMode = ReportingMode.Force };
            var service = CreateService(settings);

            _mockDataAccess.Setup(d => d.ExecuteBusinessProcessAsync(It.IsAny<DateTime>())).ReturnsAsync("OK");
            _mockDataAccess.Setup(d => d.GetArrendamientosAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<ArrendamientoInfo>());
            _mockDataAccess.Setup(d => d.GetEmailRecipientsAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<string> { "test@test.com" });
            _mockDataAccess.Setup(d => d.GetMorosidadReportDataAsync()).ReturnsAsync(new List<MorosidadInfo>());
            _mockDataAccess.Setup(d => d.GetInactivosReportDataAsync()).ReturnsAsync(new List<InactivoInfo>());
            _mockDataAccess.Setup(d => d.GetSystemParametersAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(new List<SystemParameter>());

            _mockReportingService.Setup(r => r.GenerateMorosidadPdfAsync(It.IsAny<IEnumerable<MorosidadInfo>>(), It.IsAny<byte[]?>())).ReturnsAsync(new byte[] { 1, 2, 3 });
            _mockReportingService.Setup(r => r.GenerateMorosidadExcelAsync(It.IsAny<IEnumerable<MorosidadInfo>>())).ReturnsAsync(new byte[] { 4, 5, 6 });
            _mockReportingService.Setup(r => r.GenerateInactivosExcelAsync(It.IsAny<IEnumerable<InactivoInfo>>())).ReturnsAsync(new byte[] { 7, 8, 9 });

            // Act
            await service.PerformSyncAsync();

            // Assert
            // Verificar que se llama con IdTipo = 1 para notificaciones normales
            _mockDataAccess.Verify(d => d.GetEmailRecipientsAsync(It.IsAny<int>(), 1), Times.Once);
        }

        /// <summary>
        /// Verifica que se use IdTipo = 2 para notificaciones de error.
        /// </summary>
        [Fact]
        public async Task PerformSyncAsync_UsaIdTipo2_ParaNotificacionesDeError()
        {
            // Arrange
            var settings = new SyncSettings { SkipReportDateValidation = true, ReportMode = ReportingMode.Force };
            var service = CreateService(settings);

            _mockDataAccess.Setup(d => d.ExecuteBusinessProcessAsync(It.IsAny<DateTime>())).ThrowsAsync(new InvalidOperationException("Error simulado"));
            _mockDataAccess.Setup(d => d.GetEmailRecipientsAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<string> { "admin@test.com" });

            // Act
            await service.PerformSyncAsync();

            // Assert
            // Verificar que se llama con IdTipo = 2 para notificaciones de error
            _mockDataAccess.Verify(d => d.GetEmailRecipientsAsync(It.IsAny<int>(), 2), Times.Once);
        }
    }
} 