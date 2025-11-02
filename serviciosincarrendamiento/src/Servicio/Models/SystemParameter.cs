namespace ServicioSincArrendamiento.Models
{
    public class SystemParameter
    {
        public string Codigo { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
        public string Desencriptado { get; set; } = string.Empty;
        public object? Byte { get; set; }
    }
} 