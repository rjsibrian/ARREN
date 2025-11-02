using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using ServicioSincArrendamiento.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ServicioSincArrendamiento.Services;

public class DataAccessService : IDataAccessService
{
    private readonly ILogger<DataAccessService> _logger;
    private readonly string _dbDatosConnection;
    private readonly string _controlDbConnection;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly DatabaseSettings _dbSettings;

    // Proveedor de conexión estático para permitir el mocking en pruebas unitarias
    public static Func<string, DbConnection> ConnectionProvider { get; set; } = (connectionString) => new SqlConnection(connectionString);

    public DataAccessService(ILogger<DataAccessService> logger, IConfiguration configuration, IOptions<DatabaseSettings> dbSettings)
    {
        _logger = logger;
        _dbSettings = dbSettings.Value;
        _dbDatosConnection = configuration.GetConnectionString("DbDatosConnection") 
                             ?? throw new InvalidOperationException("La cadena de conexión 'DbDatosConnection' no está configurada.");
        _controlDbConnection = configuration.GetConnectionString("ControlDbConnection")
                               ?? throw new InvalidOperationException("La cadena de conexión 'ControlDbConnection' no está configurada.");

        _retryPolicy = Policy
            .Handle<SqlException>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(5), (exception, timeSpan, retryCount, context) =>
            {
                _logger.LogWarning(exception, "Error de BD. Reintentando en {timeSpan}. Intento {retryCount}/3.", timeSpan, retryCount);
            });
    }

    private DbConnection GetConnection(string connectionString) => ConnectionProvider(connectionString);
    
    private void SetCommandTimeout(DbCommand command)
    {
        command.CommandTimeout = _dbSettings.CommandTimeoutSeconds;
    }

    public async Task<DateTime> GetLastSyncDateAsync()
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            await using var connection = GetConnection(_controlDbConnection);
            await using var command = connection.CreateCommand();
            SetCommandTimeout(command);
            command.CommandText = "SELECT MAX(fecha_ultima_sinc) FROM dbo.sincronizacion_control WHERE codigo_proceso = @processCode";
            var processCodeParam = command.CreateParameter();
            processCodeParam.ParameterName = "@processCode";
            processCodeParam.Value = "ARRENDA_POS_SYNC";
            command.Parameters.Add(processCodeParam);
            
            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();

            if (result != null && result != DBNull.Value)
            {
                return Convert.ToDateTime(result);
            }
            _logger.LogWarning("No se encontró fecha de sincronización. Se usará valor mínimo.");
            return DateTime.MinValue;
        });
    }

    public async Task<string> ExecuteBusinessProcessAsync(DateTime executionDate)
    {
        _logger.LogInformation("Ejecutando SP: Sp_DbDatos_Arrendamiento_Pos_Business");
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            await using var connection = GetConnection(_dbDatosConnection);
            await using var command = connection.CreateCommand();
            SetCommandTimeout(command);
            command.CommandText = "Sp_DbDatos_Arrendamiento_Pos_Business";
            command.CommandType = CommandType.StoredProcedure;
            
            var dateParam = command.CreateParameter();
            dateParam.ParameterName = "@Fecha";
            dateParam.Value = executionDate;
            command.Parameters.Add(dateParam);
            
            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result?.ToString() ?? "Sp_DbDatos_Arrendamiento_Pos_Business ejecutado sin mensaje.";
        });
    }

    public async Task<IEnumerable<ArrendamientoInfo>> GetArrendamientosAsync(DateTime fecha)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var arrendamientos = new List<ArrendamientoInfo>();
            await using var connection = GetConnection(_dbDatosConnection);
            await using var command = connection.CreateCommand();
            SetCommandTimeout(command);
            command.CommandText = "Comercios.Sp_Arrendamiento_Lista_gete";
            command.CommandType = CommandType.StoredProcedure;

            var fechaParam = command.CreateParameter();
            fechaParam.ParameterName = "@Fecha";
            fechaParam.Value = fecha;
            command.Parameters.Add(fechaParam);

            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                arrendamientos.Add(new ArrendamientoInfo
                {
                    Retailer = reader["Retailer"]?.ToString() ?? string.Empty,
                    Padre = reader["Padre"]?.ToString() ?? string.Empty,
                    Consolidar = reader["Consolidar"] != DBNull.Value ? Convert.ToInt32(reader["Consolidar"]) : 0,
                    Monto = reader["Monto"] != DBNull.Value ? Convert.ToDecimal(reader["Monto"]) : 0,
                    Cantidad = reader["Cantidad"] != DBNull.Value ? Convert.ToInt32(reader["Cantidad"]) : 0
                });
            }
            return arrendamientos;
        });
    }

    public async Task ExecuteSyncProcessAsync(ArrendamientoInfo arrendamiento)
    {
        if (arrendamiento == null)
        {
            throw new ArgumentNullException(nameof(arrendamiento));
        }

        await _retryPolicy.ExecuteAsync(async () =>
        {
            await using var connection = GetConnection(_dbDatosConnection);
            await using var command = connection.CreateCommand();
            SetCommandTimeout(command);
            command.CommandText = "Sp_DbDatos_Arrendamiento_Pos_Synchronize";
            command.CommandType = CommandType.StoredProcedure;
            
            command.Parameters.Add(new SqlParameter("@Retailer", arrendamiento.Retailer));
            command.Parameters.Add(new SqlParameter("@RtlPadre", arrendamiento.Padre));
            command.Parameters.Add(new SqlParameter("@Consolidar", arrendamiento.Consolidar));
            command.Parameters.Add(new SqlParameter("@Arrendamiento", arrendamiento.Monto));
            command.Parameters.Add(new SqlParameter("@NoPos", arrendamiento.Cantidad));
            command.Parameters.Add(new SqlParameter("@User", "ServicioSinc"));

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        });
    }
    
    public async Task ExecuteDisableProcessAsync()
    {
        _logger.LogInformation("Ejecutando SP: Sp_DbDatos_Arrendamiento_Pos_Disable");
        await _retryPolicy.ExecuteAsync(async () =>
        {
            await using var connection = GetConnection(_dbDatosConnection);
            await using var command = connection.CreateCommand();
            SetCommandTimeout(command);
            command.CommandText = "Sp_DbDatos_Arrendamiento_Pos_Disable";
            command.CommandType = CommandType.StoredProcedure;
            
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
            _logger.LogInformation("SP Sp_DbDatos_Arrendamiento_Pos_Disable ejecutado correctamente.");
        });
    }
    
    public async Task ExecuteAlertLoadProcessAsync()
    {
        _logger.LogInformation("Ejecutando SP: Sp_DbDatos_Arrendamiento_Pos_Alert_Load");
        await _retryPolicy.ExecuteAsync(async () =>
        {
            await using var connection = GetConnection(_dbDatosConnection);
            await using var command = connection.CreateCommand();
            SetCommandTimeout(command);
            command.CommandText = "Sp_DbDatos_Arrendamiento_Pos_Alert_Load";
            command.CommandType = CommandType.StoredProcedure;
            
            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
            _logger.LogInformation("SP Sp_DbDatos_Arrendamiento_Pos_Alert_Load ejecutado correctamente.");
        });
    }

    public async Task<IEnumerable<string>> GetEmailRecipientsAsync(int idSistema, int idTipo = 1)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var recipients = new List<string>();
            await using var connection = GetConnection(_controlDbConnection);
            await using var command = connection.CreateCommand();
            SetCommandTimeout(command);
            command.CommandText = "Mensajeria.Sp_Destination_Emails_Get";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(new SqlParameter("@IdSistema", idSistema));
            command.Parameters.Add(new SqlParameter("@IdTipo", idTipo));
            
            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var email = reader["Email"]?.ToString(); // fix production -> al cambiar dbdato a control el campo cambio de Email_Empresa a Email
                if (!string.IsNullOrEmpty(email))
                {
                    recipients.Add(email);
                }
            }
            if (!recipients.Any()) _logger.LogWarning("SP de destinatarios no devolvió resultados para IdTipo: {IdTipo}.", idTipo);
            return recipients;
        });
    }

    public async Task<IEnumerable<MorosidadInfo>> GetMorosidadReportDataAsync()
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var reportData = new List<MorosidadInfo>();
            await using var connection = GetConnection(_dbDatosConnection);
            await using var command = connection.CreateCommand();
            SetCommandTimeout(command);
            command.CommandText = "Sp_DbDatos_Arrendamiento_Pos_Alert_Get";
            command.CommandType = CommandType.StoredProcedure;

            var previousMonth = DateTime.Now.AddMonths(-1);
            command.Parameters.Add(new SqlParameter("@Tipo", 2));
            command.Parameters.Add(new SqlParameter("@Desde", new DateTime(previousMonth.Year, previousMonth.Month, 1)));
            command.Parameters.Add(new SqlParameter("@Hasta", new DateTime(previousMonth.Year, previousMonth.Month, DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month))));

            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                reportData.Add(new MorosidadInfo
                {
                    No = reportData.Count + 1,
                    Banco = reader["banco"]?.ToString() ?? string.Empty,
                    Retailer = reader["retailer"]?.ToString() ?? string.Empty,
                    Nombre = reader["nombre"]?.ToString() ?? string.Empty,
                    Monto = reader["monto"] != DBNull.Value ? Convert.ToDecimal(reader["monto"]) : 0,
                    Saldo = reader["saldo"] != DBNull.Value ? Convert.ToDecimal(reader["saldo"]) : 0,
                    Pte = reader["debe"] != DBNull.Value ? Convert.ToInt32(reader["debe"]) : 0,
                    Inicio = reader["inicio"] != DBNull.Value ? Convert.ToDateTime(reader["inicio"]) : DateTime.MinValue,
                    Mes = reader["mes"] != DBNull.Value ? Convert.ToDateTime(reader["mes"]).ToString("MM/yyyy") : string.Empty,
                    Pos = reader["pos"] != DBNull.Value ? Convert.ToInt32(reader["pos"]) : 0,
                    Estado = reader["estado"]?.ToString() ?? string.Empty,
                    Abonos = reader["abonos"] != DBNull.Value ? Convert.ToDecimal(reader["abonos"]) : 0,
                    DebC = reader["debito_c"] != DBNull.Value ? Convert.ToDecimal(reader["debito_c"]) : 0,
                    DebA = reader["debito_a"] != DBNull.Value ? Convert.ToDecimal(reader["debito_a"]) : 0,
                    Max = reader["maximo"] != DBNull.Value ? Convert.ToDecimal(reader["maximo"]) : 0
                });
            }
            return reportData;
        });
    }

    public async Task<IEnumerable<InactivoInfo>> GetInactivosReportDataAsync()
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var inactivos = new List<InactivoInfo>();
            await using var connection = GetConnection(_dbDatosConnection);
            await using var command = connection.CreateCommand();
            SetCommandTimeout(command);
            command.CommandText = "Sp_DbDatos_Arrendamiento_Pos_Disconnect_Get";
            command.CommandType = CommandType.StoredProcedure;

            var estadoParam = command.CreateParameter();
            estadoParam.ParameterName = "@Estado";
            estadoParam.Value = 0;
            command.Parameters.Add(estadoParam);

            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                inactivos.Add(new InactivoInfo
                {
                    No = inactivos.Count + 1,
                    Banco = reader["banco"]?.ToString() ?? string.Empty,
                    Retailer = reader["retailer"]?.ToString() ?? string.Empty,
                    Nombre = reader["nombre"]?.ToString() ?? string.Empty,
                    Monto = reader["monto"] != DBNull.Value ? Convert.ToDecimal(reader["monto"]) : 0,
                    Saldo = reader["saldo"] != DBNull.Value ? Convert.ToDecimal(reader["saldo"]) : 0,
                    Pte = reader["debe"] != DBNull.Value ? Convert.ToInt32(reader["debe"]) : 0,
                    Inicio = reader["inicio"] != DBNull.Value ? Convert.ToDateTime(reader["inicio"]) : DateTime.MinValue,
                    Retiro = reader["retiro"] != DBNull.Value ? Convert.ToDateTime(reader["retiro"]) : DateTime.MinValue,
                    Pos = reader["pos"] != DBNull.Value ? Convert.ToInt32(reader["pos"]) : 0,
                    Estado = reader["estado"]?.ToString() ?? string.Empty
                });
            }
            return inactivos;
        });
    }

    public async Task<IEnumerable<NuevoReporteInfo>> GetNuevoReporteDataAsync()
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var nuevoReporte = new List<NuevoReporteInfo>();
            await using var connection = GetConnection(_dbDatosConnection);
            await using var command = connection.CreateCommand();
            SetCommandTimeout(command);
            command.CommandText = "TU_STORED_PROCEDURE_AQUI"; // Cambia por el nombre de tu SP
            command.CommandType = CommandType.StoredProcedure;

            // Agregar parámetros si tu SP los necesita
            // var param1 = command.CreateParameter();
            // param1.ParameterName = "@Parametro1";
            // param1.Value = valor1;
            // command.Parameters.Add(param1);

            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                nuevoReporte.Add(new NuevoReporteInfo
                {
                    // Mapear las columnas de tu SP a las propiedades del modelo
                    Id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0,
                    Nombre = reader["nombre"]?.ToString() ?? string.Empty,
                    Fecha = reader["fecha"] != DBNull.Value ? Convert.ToDateTime(reader["fecha"]) : DateTime.MinValue,
                    Monto = reader["monto"] != DBNull.Value ? Convert.ToDecimal(reader["monto"]) : 0
                    // Agregar más mapeos según las columnas de tu SP
                });
            }
            return nuevoReporte;
        });
    }

    public async Task<IEnumerable<SystemParameter>> GetSystemParametersAsync(int idSistema, string phrase)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var systemParameters = new List<SystemParameter>();
            await using var connection = GetConnection(_controlDbConnection);
            await using var command = connection.CreateCommand();
            SetCommandTimeout(command);
            command.CommandText = "Sistemas.Sp_Parameters_Get";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@idSistema", idSistema));
            command.Parameters.Add(new SqlParameter("@Phrase", phrase));

            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                systemParameters.Add(new SystemParameter
                {
                    Codigo = reader["Codigo"]?.ToString() ?? string.Empty,
                    Valor = reader["Valor"]?.ToString() ?? string.Empty,
                    Desencriptado = reader["Desencriptado"]?.ToString() ?? string.Empty,
                    Byte = reader["Byte"] != DBNull.Value ? (byte[])reader["Byte"] : null
                });
            }
            return systemParameters;
        });
    }

    public async Task<bool> UpdateSystemParameterAsync(string code, string description, bool isEncryption, int idSistema, string phrase)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(description) || idSistema <= 0)
        {
            _logger.LogWarning("UpdateSystemParameterAsync: Parámetros inválidos. Code: {Code}, Desc: {Desc}, SystemId: {SystemId}", code, description, idSistema);
            return false;
        }

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            await using var connection = GetConnection(_controlDbConnection);
            await using var command = connection.CreateCommand();
            SetCommandTimeout(command);
            command.CommandText = "Sistemas.Sp_Parameters_Update";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(new SqlParameter("@Codigo", code));
            command.Parameters.Add(new SqlParameter("@Descripcion", description));
            command.Parameters.Add(new SqlParameter("@IsEncryption", isEncryption));
            command.Parameters.Add(new SqlParameter("@IdSistema", idSistema));
            command.Parameters.Add(new SqlParameter("@Phrase", phrase));

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
            _logger.LogInformation("Parámetro del sistema '{Code}' actualizado.", code);
            return true;
        });
    }
}