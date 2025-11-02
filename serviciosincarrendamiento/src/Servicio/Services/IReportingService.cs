using System.Collections.Generic;
using System.Threading.Tasks;
using ServicioSincArrendamiento.Models;

namespace ServicioSincArrendamiento.Services
{
    public interface IReportingService
    {
        Task<byte[]> GenerateMorosidadPdfAsync(IEnumerable<MorosidadInfo> data, byte[]? logo);
        Task<byte[]> GenerateMorosidadExcelAsync(IEnumerable<MorosidadInfo> data);
        Task<byte[]> GenerateInactivosExcelAsync(IEnumerable<InactivoInfo> data);
    }
} 