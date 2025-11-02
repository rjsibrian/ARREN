using Xunit;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using ServicioSincArrendamiento.Services;
using ServicioSincArrendamiento.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using System.Threading;
using System.Data;
using System.Linq;
using Moq.Protected;

namespace UnitTest
{
    /// <summary>
    /// Pruebas unitarias para DataAccessService.
    /// Cada prueba está aislada y crea sus propias dependencias para evitar interferencias.
    /// </summary>
    public class DataAccessServiceTests
    {
        #region Helpers

        /// <summary>
        /// Crea un mock de IConfiguration con las cadenas de conexión especificadas.
        /// </summary>
        private static Mock<IConfiguration> CreateMockConfiguration(string? dbDatos, string? controlDb)
        {
            var mockConfig = new Mock<IConfiguration>();
            var mockSection = new Mock<IConfigurationSection>();
            mockSection.Setup(s => s["DbDatosConnection"]).Returns(dbDatos);
            mockSection.Setup(s => s["ControlDbConnection"]).Returns(controlDb);
            mockConfig.Setup(c => c.GetSection("ConnectionStrings")).Returns(mockSection.Object);
            return mockConfig;
        }

        /// <summary>
        /// Configura los mocks básicos de DbConnection y DbCommand.
        /// </summary>
        private static (Mock<DbConnection>, Mock<DbCommand>) SetupBasicMocks()
        {
            var mockConnection = new Mock<DbConnection>();
            var mockCommand = new Mock<DbCommand>();
            var mockParams = new Mock<DbParameterCollection>();
            var mockParam = new Mock<DbParameter>();

            mockConnection.Protected().Setup<DbCommand>("CreateDbCommand").Returns(mockCommand.Object);
            mockCommand.Protected().SetupGet<DbParameterCollection>("DbParameterCollection").Returns(mockParams.Object);
            mockCommand.Protected().Setup<DbParameter>("CreateDbParameter").Returns(mockParam.Object);
            
            DataAccessService.ConnectionProvider = _ => mockConnection.Object;
            
            return (mockConnection, mockCommand);
        }

        /// <summary>
        /// Crea un mock de DbDataReader para simular respuestas de la base de datos.
        /// </summary>
        private static Mock<DbDataReader> CreateMockDataReader(List<Dictionary<string, object>> rows)
        {
            var mockReader = new Mock<DbDataReader>();
            int currentRow = -1;

            // Simular el avance del lector
            mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                .Returns(() => {
                    currentRow++;
                    return Task.FromResult(currentRow < rows.Count);
                });

            // Simular el acceso a los datos por nombre de columna
            // Esto funcionará porque el ReadAsync simulado arriba controla el "puntero" de la fila actual.
            mockReader.Setup(r => r[It.IsAny<string>()])
                .Returns((string columnName) => {
                    if (currentRow < 0 || currentRow >= rows.Count)
                    {
                        // Esto simula el comportamiento de un lector real si intentas leer antes del primer Read o después del último.
                        throw new InvalidOperationException("No data exists for the row/column.");
                    }
                    return rows[currentRow].ContainsKey(columnName) ? rows[currentRow][columnName] : DBNull.Value;
                });
            
            return mockReader;
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_ConConfiguracionValida_CreaInstancia()
        {
            // Arrange
            var mockConfig = CreateMockConfiguration("valid_db", "valid_control");
            SetupBasicMocks();

            // Act
            var service = new DataAccessService(NullLogger<DataAccessService>.Instance, mockConfig.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void Constructor_ConConnectionStringNula_LanzaExcepcion()
        {
            // Arrange
            var mockConfig = CreateMockConfiguration("valid_db", null);
            SetupBasicMocks();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                new DataAccessService(NullLogger<DataAccessService>.Instance, mockConfig.Object));
            
            Assert.Equal("La cadena de conexión 'ControlDbConnection' no está configurada.", ex.Message);
        }

        #endregion

        #region GetArrendamientosAsync Tests

        [Fact]
        public async Task GetArrendamientosAsync_ConDatos_RetornaLista()
        {
            // Arrange
            var mockConfig = CreateMockConfiguration("valid_db", "valid_control");
            var (_, mockCommand) = SetupBasicMocks();
            var service = new DataAccessService(NullLogger<DataAccessService>.Instance, mockConfig.Object);

            var data = new List<Dictionary<string, object>>
            {
                new() {
                    { "Retailer", "R001" }, { "Padre", "P001" }, { "Consolidar", 1 },
                    { "Monto", 1500.00m }, { "Cantidad", 100 }
                }
            };
            var mockReader = CreateMockDataReader(data);
            
            mockCommand.Protected()
                .Setup<Task<DbDataReader>>("ExecuteDbDataReaderAsync", It.IsAny<CommandBehavior>(), It.IsAny<CancellationToken>())
                .ReturnsAsync(mockReader.Object);

            // Act
            var result = await service.GetArrendamientosAsync(DateTime.Now);

            // Assert
            var arrendamiento = Assert.Single(result);
            Assert.Equal("R001", arrendamiento.Retailer);
            Assert.Equal(1500.00m, arrendamiento.Monto);
        }

        [Fact]
        public async Task GetArrendamientosAsync_SinDatos_RetornaListaVacia()
        {
            // Arrange
            var mockConfig = CreateMockConfiguration("valid_db", "valid_control");
            var (_, mockCommand) = SetupBasicMocks();
            var service = new DataAccessService(NullLogger<DataAccessService>.Instance, mockConfig.Object);
            
            var mockReader = CreateMockDataReader(new List<Dictionary<string, object>>());
            mockCommand.Protected()
                .Setup<Task<DbDataReader>>("ExecuteDbDataReaderAsync", It.IsAny<CommandBehavior>(), It.IsAny<CancellationToken>())
                .ReturnsAsync(mockReader.Object);

            // Act
            var result = await service.GetArrendamientosAsync(DateTime.Now);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region UpdateSystemParameterAsync Tests

        [Fact]
        public async Task UpdateSystemParameterAsync_ConDatosValidos_RetornaTrue()
        {
            // Arrange
            var mockConfig = CreateMockConfiguration("valid_db", "valid_control");
            var (_, mockCommand) = SetupBasicMocks();
            var service = new DataAccessService(NullLogger<DataAccessService>.Instance, mockConfig.Object);
            
            mockCommand.Setup(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await service.UpdateSystemParameterAsync("code", "desc", false, 1, "phrase");

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("", "desc", 1)]
        [InlineData("code", "", 1)]
        [InlineData("code", "desc", 0)]
        public async Task UpdateSystemParameterAsync_ConParametrosInvalidos_RetornaFalse(string code, string desc, int id)
        {
            // Arrange
            var mockConfig = CreateMockConfiguration("valid_db", "valid_control");
            SetupBasicMocks();
            var service = new DataAccessService(NullLogger<DataAccessService>.Instance, mockConfig.Object);
            
            // Act
            var result = await service.UpdateSystemParameterAsync(code, desc, false, id, "phrase");

            // Assert
            Assert.False(result);
        }
        
        #endregion

        #region ReportDataAsync Tests

        [Fact]
        public async Task GetMorosidadReportDataAsync_ConDatos_RetornaLista()
        {
            // Arrange
            var mockConfig = CreateMockConfiguration("valid_db", "valid_control");
            var (_, mockCommand) = SetupBasicMocks();
            var service = new DataAccessService(NullLogger<DataAccessService>.Instance, mockConfig.Object);

            var data = new List<Dictionary<string, object>>
            {
                new() {
                    { "banco", "Banco Test" }, { "retailer", "RTL123" }, { "nombre", "Nombre Test" },
                    { "monto", 5000m }, { "saldo", 2500m }
                }
            };
            var mockReader = CreateMockDataReader(data);
            
            mockCommand.Protected()
                .Setup<Task<DbDataReader>>("ExecuteDbDataReaderAsync", It.IsAny<CommandBehavior>(), It.IsAny<CancellationToken>())
                .ReturnsAsync(mockReader.Object);
            
            // Act
            var result = await service.GetMorosidadReportDataAsync();
            
            // Assert
            var item = Assert.Single(result);
            Assert.Equal("Banco Test", item.Banco);
            Assert.Equal("RTL123", item.Retailer);
        }

        #endregion
    }
} 