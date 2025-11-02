using ServicioSincArrendamiento.Models;
using ServicioSincArrendamiento.Services;
using Microsoft.Extensions.Options;

namespace ServicioSincArrendamiento;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ISyncService _syncService;
    private readonly SyncSettings _syncSettings;

    public Worker(
        ILogger<Worker> logger, 
        ISyncService syncService,
        IOptions<SyncSettings> syncSettings)
    {
        _logger = logger;
        _syncService = syncService;
        _syncSettings = syncSettings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de Sincronización iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var executionTime = TimeSpan.Parse(_syncSettings.ExecutionTime);
                var now = DateTime.Now;
                var nextRunTime = now.Date.Add(executionTime);

                if (now > nextRunTime)
                {
                    nextRunTime = nextRunTime.AddDays(1);
                }

                var delay = nextRunTime - now;

                _logger.LogInformation("Próxima ejecución programada para: {NextRunTime}", nextRunTime);
                await Task.Delay(delay, stoppingToken);

                _logger.LogInformation("Iniciando ciclo de trabajo a las: {time}", DateTimeOffset.Now);

                await _syncService.PerformSyncAsync();

                _logger.LogInformation("Ciclo de trabajo finalizado. Esperando hasta la próxima ejecución programada.");
            }
            catch (OperationCanceledException)
            {
                // El servicio se está deteniendo, es una excepción esperada.
                break;
            }
            catch (FormatException ex)
            {
                _logger.LogCritical(ex, "El formato de 'ExecutionTime' en la configuración es inválido. Por favor, use 'HH:mm'. El servicio se detendrá.");
                break; 
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Ha ocurrido un error no controlado en el ciclo principal del worker. Se reintentará en la próxima ejecución programada.");
                // Opcional: añadir un pequeño delay para evitar loops rápidos en caso de errores de lógica.
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Servicio de Sincronización detenido.");
    }
}
