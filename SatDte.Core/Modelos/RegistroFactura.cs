namespace SatDte.Core.Modelos;

/// <summary>Una solicitud recibida y la decisión tomada (aceptada o rechazada).</summary>
public sealed class RegistroFactura
{
    public long NumeroSolicitud { get; set; }
    public DateTime FechaRecepcionUtc { get; set; }
    public string? TiempoOriginal { get; set; }
    public DateTime? FechaDocumento { get; set; }
    public string? Referencia { get; set; }
    public string? NitEmisor { get; set; }
    public string? NitReceptor { get; set; }
    public decimal? Valor { get; set; }
    public decimal? Iva { get; set; }
    public decimal? Total { get; set; }
    public bool Aprobada { get; set; }
    public long? CodigoAprobacion { get; set; }
    public string[] Errores { get; set; } = Array.Empty<string>();
}
