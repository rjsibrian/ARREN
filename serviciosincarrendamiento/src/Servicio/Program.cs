using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using ServicioSincArrendamiento.Models;
using ServicioSincArrendamiento.Services;
using Servicio.Services;
using System;
using System.IO;

namespace ServicioSincArrendamiento
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Establecer la ruta base en el directorio del ejecutable.
            // Es crucial para que el servicio de Windows encuentre los archivos de configuración.
            var processModule = System.Diagnostics.Process.GetCurrentProcess().MainModule;
            if (processModule != null)
            {
                var pathToExe = processModule.FileName;
                var pathToContentRoot = Path.GetDirectoryName(pathToExe);
                if (!string.IsNullOrEmpty(pathToContentRoot))
                {
                    Directory.SetCurrentDirectory(pathToContentRoot);
                }
            }

            // Bootstrap logger inicial - SOLO para capturar errores antes de cargar appsettings.json
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                Log.Information("Iniciando el servicio...");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "El host del servicio ha terminado de forma inesperada.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService(options =>
                {
                    options.ServiceName = "ServicioSincArrendamiento";
                })
                .UseSerilog((context, services, configuration) => 
                {
                    // Configuración COMPLETA desde appsettings.json - sin sobrescribir nada
                    configuration.ReadFrom.Configuration(context.Configuration);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Inyecta las configuraciones de appsettings.json
                    services.Configure<EmailSettings>(hostContext.Configuration.GetSection("EmailSettings"));
                    services.Configure<SyncSettings>(hostContext.Configuration.GetSection("SyncSettings"));
                    services.Configure<AppSettings>(hostContext.Configuration.GetSection("AppSettings"));
                    services.Configure<DatabaseSettings>(hostContext.Configuration.GetSection("DatabaseSettings"));

                    // Inyecta los servicios de la aplicacion
                    services.AddSingleton<IDataAccessService, DataAccessService>();
                    services.AddSingleton<IReportingService, ReportingService>();
                    services.AddSingleton<INotificationService, NotificationService>();
                    services.AddSingleton<ISyncService, SyncService>();
                    services.AddTransient<IEmailSender, EmailSender>();

                    // Anade el Worker que es el servicio en segundo plano
                    services.AddHostedService<Worker>();
                });
    }
}
