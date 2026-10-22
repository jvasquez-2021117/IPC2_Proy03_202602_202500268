using System.Text.Json.Serialization;

namespace SatDte.Core.Modelos;

/// <summary>Formato de respuesta EXACTO solicitado para actualizaciones RTU.</summary>
public sealed class RespuestaRtu
{
    [JsonPropertyName("CONTRIBUYENTES_NUEVOS")]
    public int ContribuyentesNuevos { get; set; }

    [JsonPropertyName("CONTRIBUYENTES_ACTUALIZADOS")]
    public int ContribuyentesActualizados { get; set; }

    [JsonPropertyName("NITS_INVALIDOS")]
    public int NitsInvalidos { get; set; }
}
