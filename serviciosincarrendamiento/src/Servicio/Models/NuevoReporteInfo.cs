namespace ServicioSincArrendamiento.Models
{
    public class NuevoReporteInfo
    {
        // Aquí defines las propiedades según las columnas que devuelve tu SP
        // Ejemplo:
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public decimal Monto { get; set; }
        // Agrega más propiedades según necesites
    }
}