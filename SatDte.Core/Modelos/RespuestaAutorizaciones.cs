using System.Text.Json.Serialization;

namespace SatDte.Core.Modelos;

/// <summary>El JSON de salida usa exactamente el esquema obligatorio del enunciado.</summary>
public sealed class RespuestaAutorizaciones
{
    [JsonPropertyName("resultadoAutorizaciones")]
    public ResultadoAutorizaciones ResultadoAutorizaciones { get; set; } = new();
}

public sealed class ResultadoAutorizaciones
{
    [JsonPropertyName("cantidadAutorizacionesAprobadas")]
    public int CantidadAutorizacionesAprobadas { get; set; }

    [JsonPropertyName("cantidadAutorizacionesRechazadasPorNitEmisorInexistente")]
    public int CantidadAutorizacionesRechazadasPorNitEmisorInexistente { get; set; }

    [JsonPropertyName("cantidadAutorizacionesRechazadasPorNitReceptorInexistente")]
    public int CantidadAutorizacionesRechazadasPorNitReceptorInexistente { get; set; }

    [JsonPropertyName("detalleAprobaciones")]
    public List<DetalleAprobacion> DetalleAprobaciones { get; set; } = new();
}

public sealed class DetalleAprobacion
{
    [JsonPropertyName("referencia")]
    public string Referencia { get; set; } = string.Empty;

    [JsonPropertyName("codigoAprobacion")]
    public long CodigoAprobacion { get; set; }
}
