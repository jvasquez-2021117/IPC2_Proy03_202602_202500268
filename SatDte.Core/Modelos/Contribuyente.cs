using System.Text.Json.Serialization;

namespace SatDte.Core.Modelos;

/// <summary>Entidad que representa un registro válido del RTU.</summary>
public sealed class Contribuyente
{
    [JsonPropertyName("NIT")]
    public string Nit { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    public Contribuyente() { }

    public Contribuyente(string nit, string nombre)
    {
        Nit = nit;
        Nombre = nombre;
    }
}
