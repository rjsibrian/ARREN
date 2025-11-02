using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ServicioSincArrendamiento;
using ServicioSincArrendamiento.Models;
using ServicioSincArrendamiento.Services;
using System.Globalization;

namespace UnitTest
{
    public class WorkerTests
    {
        private readonly Mock<ILogger<Worker>> _mockLogger;
        private readonly Mock<ISyncService> _mockSyncService;
        private readonly Mock<IOptions<SyncSettings>> _mockSyncSettings;

        public WorkerTests()
        {
            _mockLogger = new Mock<ILogger<Worker>>();
            _mockSyncService = new Mock<ISyncService>();
            _mockSyncSettings = new Mock<IOptions<SyncSettings>>();
        }

        private Worker CreateWorker(SyncSettings settings)
        {
            _mockSyncSettings.Setup(s => s.Value).Returns(settings);
            return new Worker(
                _mockLogger.Object,
                _mockSyncService.Object,
                _mockSyncSettings.Object
            );
        }

        /// <summary>
        /// Valida el comportamiento principal del Worker.
        /// Simula una configuración de tiempo válida y verifica que, después de esperar el delay
        /// correspondiente, el Worker llama correctamente al método `PerformSyncAsync` para
        /// iniciar la lógica de negocio.
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_DebeLlamarAlSyncService_DespuesDelDelayCalculado()
        {
            // Arrange
            // Configurar la ejecución para que ocurra en un futuro muy cercano para que la prueba sea rápida.
            var executionTime = DateTime.Now.AddSeconds(2);
            var settings = new SyncSettings { ExecutionTime = executionTime.ToString("HH:mm:ss") };
            var worker = CreateWorker(settings);

            // Usar un CancellationToken que se cancele después de un tiempo para que la prueba no se ejecute indefinidamente.
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(3)); // Cancelar después de 3 segundos

            // Act
            await worker.StartAsync(cts.Token);
            await Task.Delay(TimeSpan.FromSeconds(4), CancellationToken.None); // Dar tiempo a que se ejecute y se cancele
            await worker.StopAsync(CancellationToken.None);
            
            // Assert
            // Verificar que el servicio de sincronización fue llamado al menos una vez.
            _mockSyncService.Verify(s => s.PerformSyncAsync(), Times.AtLeastOnce);
        }

        /// <summary>
        /// Valida la robustez del Worker ante una configuración incorrecta.
        /// Simula que el formato de la hora en la configuración es inválido y verifica que
        /// el Worker registra un error crítico y se detiene, sin llegar a llamar nunca
        /// al servicio de sincronización.
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_DebeLoguearErrorCriticoYParar_ConFormatoDeHoraInvalido()
        {
            // Arrange
            // Configurar un formato de hora inválido.
            var settings = new SyncSettings { ExecutionTime = "FormatoInvalido" };
            var worker = CreateWorker(settings);

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(1)); // Cancelar rápidamente

            // Act
            await worker.StartAsync(cts.Token);
            await Task.Delay(TimeSpan.FromSeconds(2), CancellationToken.None); // Dar tiempo a que el worker procese y falle
            await worker.StopAsync(CancellationToken.None);

            // Assert
            // Verificar que el servicio de sincronización NUNCA fue llamado.
            _mockSyncService.Verify(s => s.PerformSyncAsync(), Times.Never);
            
            // Verificar que se registró un error crítico. Moq no puede verificar directamente los métodos de extensión de ILogger,
            // pero podemos verificar que se hizo una llamada al método Log con el nivel crítico.
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Critical,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("El formato de 'ExecutionTime' en la configuración es inválido. Por favor, use 'HH:mm'. El servicio se detendrá.")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
} 