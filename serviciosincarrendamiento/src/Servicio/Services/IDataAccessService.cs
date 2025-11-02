using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServicioSincArrendamiento.Models;

namespace ServicioSincArrendamiento.Services;

public interface IDataAccessService
{
    Task<DateTime> GetLastSyncDateAsync();
    Task<string> ExecuteBusinessProcessAsync(DateTime executionDate);
    Task<IEnumerable<ArrendamientoInfo>> GetArrendamientosAsync(DateTime fecha);
    Task ExecuteSyncProcessAsync(ArrendamientoInfo arrendamiento);
    Task ExecuteDisableProcessAsync();
    Task ExecuteAlertLoadProcessAsync();
            Task<IEnumerable<string>> GetEmailRecipientsAsync(int idSistema, int idTipo = 1);
    Task<IEnumerable<MorosidadInfo>> GetMorosidadReportDataAsync();
    Task<IEnumerable<InactivoInfo>> GetInactivosReportDataAsync();
    Task<IEnumerable<NuevoReporteInfo>> GetNuevoReporteDataAsync(); // Nuevo método para tu consulta
    Task<IEnumerable<SystemParameter>> GetSystemParametersAsync(int idSistema, string phrase);
    Task<bool> UpdateSystemParameterAsync(string code, string description, bool isEncryption, int idSistema, string phrase);
}