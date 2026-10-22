using System.Text.Json.Serialization;

namespace SatDte.Core.Modelos;

/// <summary>Formato de entrada: { "NIT": "...", "nombre": "..." }.</summary>
public sealed class ContribuyenteEntrada
{
    [JsonPropertyName("NIT")]
    public string? Nit { get; set; }

    [JsonPropertyName("nombre")]
    public string? Nombre { get; set; }
}
