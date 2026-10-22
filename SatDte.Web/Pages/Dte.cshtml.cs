using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SatDte.Web.Pages;

public sealed class DteModel : PageModel
{
    private readonly IHttpClientFactory _clientes;
    public DteModel(IHttpClientFactory clientes) => _clientes = clientes;

    [BindProperty] public IFormFile? Archivo { get; set; }
    public string? RespuestaJson { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Archivo is null || Archivo.Length < 1)
        {
            Error = "Selecciona un archivo .json con una o varias facturas.";
            return Page();
        }
        if (!string.Equals(Path.GetExtension(Archivo.FileName), ".json", StringComparison.OrdinalIgnoreCase) ||
            Archivo.Length > 2 * 1024 * 1024)
        {
            Error = "Solo se aceptan archivos .json de hasta 2 MB.";
            return Page();
        }
        try
        {
            using MemoryStream flujo = new();
            await Archivo.CopyToAsync(flujo);
            byte[] mensajeOriginal = flujo.ToArray();
            using (JsonDocument documento = JsonDocument.Parse(mensajeOriginal))
            {
                if (documento.RootElement.ValueKind != JsonValueKind.Array)
                {
                    Error = "El JSON debe contener un arreglo de solicitudes DTE.";
                    return Page();
                }
            }
            using HttpClient cliente = _clientes.CreateClient("SatApi");
            using ByteArrayContent contenido = new(mensajeOriginal);
            contenido.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            using HttpResponseMessage respuesta = await cliente.PostAsync("api/dte/autorizar", contenido);
            string texto = await respuesta.Content.ReadAsStringAsync();
            if (!respuesta.IsSuccessStatusCode)
            {
                Error = $"API HTTP {(int)respuesta.StatusCode}: {texto}";
                return Page();
            }
            using JsonDocument parsed = JsonDocument.Parse(texto);
            RespuestaJson = JsonSerializer.Serialize(parsed.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException ex) { Error = "JSON inválido: " + ex.Message; }
        catch (HttpRequestException) { Error = "No se puede conectar a la API en localhost:5175."; }
        catch (TaskCanceledException) { Error = "La API no respondió a tiempo."; }
        catch (IOException ex) { Error = "No se pudo leer el archivo: " + ex.Message; }
        return Page();
    }
}
