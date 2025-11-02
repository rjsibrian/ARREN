namespace ServicioSincArrendamiento.Models;

public class ArrendamientoInfo
{
    public string Retailer { get; set; } = string.Empty;
    public string Padre { get; set; } = string.Empty;
    public int Consolidar { get; set; }
    public decimal Monto { get; set; }
    public int Cantidad { get; set; }
    public int Id { get; set; }
    public int NumUnico { get; set; }
} 