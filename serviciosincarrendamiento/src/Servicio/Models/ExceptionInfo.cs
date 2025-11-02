namespace ServicioSincArrendamiento.Models
{
    public class ExceptionInfo
    {
        public string? Sistema { get; set; }
        public string? Usuario { get; set; }
        public string? Funcion { get; set; }
        public string? Excepcion { get; set; }
        public string? StackTrace { get; set; }
        public DateTime FechaOcurrencia { get; set; } = DateTime.Now;
        public string? InformacionAdicional { get; set; }
    }
} 