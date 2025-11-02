using ServicioSincArrendamiento.Models;
using System.Diagnostics;

namespace ServicioSincArrendamiento.Services
{
    public static class ExceptionHelper
    {
        public static ExceptionInfo CreateExceptionInfo(
            Exception ex, 
            string sistema, 
            string funcion, 
            string? usuario = null,
            string? informacionAdicional = null)
        {
            return new ExceptionInfo
            {
                Sistema = sistema,
                Usuario = usuario ?? "Sistema",
                Funcion = funcion,
                Excepcion = ex.Message,
                StackTrace = ex.StackTrace,
                FechaOcurrencia = DateTime.Now,
                InformacionAdicional = informacionAdicional
            };
        }

        public static ExceptionInfo CreateExceptionInfoFromCurrentContext(
            Exception ex,
            string? informacionAdicional = null)
        {
            var stackTrace = new StackTrace(ex, true);
            var frame = stackTrace.GetFrame(1); // Obtener el frame que llamó al método
            
            return new ExceptionInfo
            {
                Sistema = "ServicioSincArrendamiento",
                Usuario = "Sistema",
                Funcion = frame?.GetMethod()?.Name ?? "Método Desconocido",
                Excepcion = ex.Message,
                StackTrace = ex.StackTrace,
                FechaOcurrencia = DateTime.Now,
                InformacionAdicional = informacionAdicional
            };
        }
    }
} 