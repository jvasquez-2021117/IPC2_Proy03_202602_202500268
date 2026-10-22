using System.Text;
using System.Text.Json;
using SatDte.Core.Modelos;

namespace SatDte.Core.Persistencia;

/// <summary>Almacén en archivo JSON. Escritura mediante archivo temporal y reemplazo.</summary>
public sealed class ArchivoRtu
{
    private readonly string _ruta;
    private static readonly JsonSerializerOptions Opciones = new()
    {
        WriteIndented = true
    };

    public ArchivoRtu(string ruta)
    {
        _ruta = ruta;
    }

    public Contribuyente[] Leer()
    {
        if (!File.Exists(_ruta))
            return Array.Empty<Contribuyente>();

        string contenido = File.ReadAllText(_ruta, Encoding.UTF8);
        Contribuyente[]? registros = JsonSerializer.Deserialize<Contribuyente[]>(contenido);
        return registros ?? throw new InvalidDataException("El archivo RTU contiene JSON nulo.");
    }

    public void Guardar(Contribuyente[] registros)
    {
        string? carpeta = Path.GetDirectoryName(_ruta);
        if (!string.IsNullOrEmpty(carpeta))
            Directory.CreateDirectory(carpeta);

        string temporal = _ruta + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            string json = JsonSerializer.Serialize(registros, Opciones);
            File.WriteAllText(temporal, json, new UTF8Encoding(false));
            File.Move(temporal, _ruta, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporal)) File.Delete(temporal);
        }
    }
}
