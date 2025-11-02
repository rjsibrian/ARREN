namespace ServicioSincArrendamiento.Models;

public class EmailAttachment
{
    public required string FileName { get; set; }
    public required byte[] Data { get; set; }
    public string ContentType { get; set; } = string.Empty; // e.g., "application/pdf"
} 