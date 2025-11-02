namespace ServicioSincArrendamiento.Models;

public class MorosidadInfo
{
    public int No { get; set; }
    public string Banco { get; set; } = string.Empty;
    public string Retailer { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public decimal Saldo { get; set; }
    public int Pte { get; set; } // Meses pendientes
    public DateTime Inicio { get; set; }
    public string Mes { get; set; } = string.Empty;
    public int Pos { get; set; }
    public string Estado { get; set; } = string.Empty;
    public decimal Abonos { get; set; }
    public decimal DebC { get; set; } // Débito por Contracargos
    public decimal DebA { get; set; } // Débito por Arrendamiento
    public decimal Max { get; set; } // Pago Máximo
} 