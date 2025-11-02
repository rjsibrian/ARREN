using System.Text.Json.Serialization;

namespace ServicioSincArrendamiento.Models;

public enum ReportingMode
{
    // Solo envía reportes si existen datos para TODOS los tipos de reporte.
    Strict,
    // Envía los reportes para los que encuentre datos (al menos uno).
    Flexible,
    // Envía siempre todos los reportes, aunque estén vacíos.
    Force,
    None
}
 
public class SyncSettings
{
    public string ExecutionTime { get; set; } = "23:00";
    public int AdvanceDays { get; set; }
    public bool SkipReportDateValidation { get; set; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ReportingMode ReportMode { get; set; } = ReportingMode.Flexible;
} 