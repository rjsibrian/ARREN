namespace ServicioSincArrendamiento.Models
{
    /// <summary>
    /// Contiene la configuración para la interacción con la base de datos.
    /// </summary>
    public class DatabaseSettings
    {
        /// <summary>
        /// El tiempo en segundos que un comando SQL puede ejecutarse antes de que se considere un timeout.
        /// El valor por defecto de SqlClient es 30 segundos.
        /// </summary>
        public int CommandTimeoutSeconds { get; set; } = 240; // Default a 240 segundos(4 minutos) si no está en el JSON.
    }
} 