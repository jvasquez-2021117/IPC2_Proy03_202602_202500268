using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SatDte.Web.Pages;

public sealed class RtuModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public RtuModel(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    [BindProperty]
    public IFormFile? Archivo { get; set; }

    public string? RespuestaJson { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Archivo is null || Archivo.Length == 0)
        {
            Error = "Selecciona un archivo JSON que no esté vacío.";
            return Page();
        }

        if (!string.Equals(Path.GetExtension(Archivo.FileName), ".json", StringComparison.OrdinalIgnoreCase))
        {
            Error = "El archivo debe tener la extensión .json.";
            return Page();
        }

        if (Archivo.Length > 2 * 1024 * 1024)
        {
            Error = "El archivo no puede superar los 2 MB.";
            return Page();
        }

        try
        {
            using MemoryStream memoria = new();
            await Archivo.CopyToAsync(memoria);
            byte[] bytesOriginales = memoria.ToArray();

            // Comprobar forma, no modificar ni reescribir el mensaje de entrada.
            using (JsonDocument documento = JsonDocument.Parse(bytesOriginales))
            {
                if (documento.RootElement.ValueKind != JsonValueKind.Array)
                {
                    Error = "La raíz del JSON debe ser un arreglo de contribuyentes.";
                    return Page();
                }
            }

            using HttpClient cliente = _httpClientFactory.CreateClient("SatApi");
            using ByteArrayContent contenido = new(bytesOriginales);
            contenido.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            using HttpResponseMessage respuesta = await cliente.PostAsync("api/rtu/importar", contenido);
            string mensaje = await respuesta.Content.ReadAsStringAsync();

            if (!respuesta.IsSuccessStatusCode)
            {
                Error = $"La API respondió {(int)respuesta.StatusCode}: {mensaje}";
                return Page();
            }

            using JsonDocument respuestaJson = JsonDocument.Parse(mensaje);
            RespuestaJson = JsonSerializer.Serialize(respuestaJson.RootElement,
                new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException ex)
        {
            Error = "El archivo contiene JSON inválido: " + ex.Message;
        }
        catch (HttpRequestException)
        {
            Error = "No se logró conectar con la API. Confirma que SatDte.Api se esté ejecutando en http://localhost:5175.";
        }
        catch (TaskCanceledException)
        {
            Error = "La API tardó demasiado en responder. Revisa que esté ejecutándose.";
        }
        catch (IOException ex)
        {
            Error = "No se pudo leer el archivo: " + ex.Message;
        }

        return Page();
    }
}
