using System.Text;
using System.Text.Json;
using SatDte.Core.Modelos;

namespace SatDte.Core.Persistencia;

/// <summary>Archivo de todas las solicitudes, con escritura temporal + reemplazo.</summary>
public sealed class ArchivoFacturas
{
    private readonly string _ruta;
    private static readonly JsonSerializerOptions Opciones = new() { WriteIndented = true };

    public ArchivoFacturas(string ruta) => _ruta = ruta;

    public RegistroFactura[] Leer()
    {
        if (!File.Exists(_ruta)) return Array.Empty<RegistroFactura>();
        RegistroFactura[]? datos = JsonSerializer.Deserialize<RegistroFactura[]>(File.ReadAllText(_ruta, Encoding.UTF8));
        return datos ?? throw new InvalidDataException("El archivo de solicitudes contiene JSON nulo.");
    }

    public void Guardar(RegistroFactura[] datos)
    {
        string? carpeta = Path.GetDirectoryName(_ruta);
        if (!string.IsNullOrEmpty(carpeta)) Directory.CreateDirectory(carpeta);
        string temporal = _ruta + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporal, JsonSerializer.Serialize(datos, Opciones), new UTF8Encoding(false));
            File.Move(temporal, _ruta, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporal)) File.Delete(temporal);
        }
    }
}
