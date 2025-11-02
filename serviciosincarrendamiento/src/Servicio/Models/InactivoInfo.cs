namespace ServicioSincArrendamiento.Models;

public class InactivoInfo
{
    public int No { get; set; }
    public string Banco { get; set; } = string.Empty;
    public string Retailer { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public decimal Saldo { get; set; }
    public int Pte { get; set; } // Meses pendientes
    public DateTime Inicio { get; set; }
    public DateTime Retiro { get; set; }
    public int Pos { get; set; }
    public string Estado { get; set; } = string.Empty;
} 