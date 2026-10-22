using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SatDte.Web.Pages;

/// <summary>Consultas GET al servicio externo; no se leen archivos del backend directamente.</summary>
public abstract class ConsultaApiPageModel : PageModel
{
    private readonly IHttpClientFactory _clientes;
    protected ConsultaApiPageModel(IHttpClientFactory clientes) => _clientes = clientes;

    public string? Error { get; protected set; }
    public bool ConsultaRealizada { get; protected set; }

    protected bool FechaValida(string? texto) =>
        DateOnly.TryParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _);

    protected bool ValidarFechas(string? inicio, string? fin)
    {
        if (!DateOnly.TryParseExact(inicio, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly desde) ||
            !DateOnly.TryParseExact(fin, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly hasta) || desde > hasta)
        {
            Error = "Selecciona dos fechas válidas; la fecha inicial no debe superar a la final.";
            return false;
        }
        return true;
    }

    protected async Task<T?> ObtenerAsync<T>(string ruta)
    {
        ConsultaRealizada = true;
        try
        {
            using HttpClient cliente = _clientes.CreateClient("SatApi");
            using HttpResponseMessage respuesta = await cliente.GetAsync(ruta);
            if (!respuesta.IsSuccessStatusCode)
            {
                string texto = await respuesta.Content.ReadAsStringAsync();
                using JsonDocument json = JsonDocument.Parse(texto);
                Error = json.RootElement.TryGetProperty("error", out JsonElement descripcion)
                    ? descripcion.GetString() : $"La API respondió HTTP {(int)respuesta.StatusCode}.";
                return default;
            }
            return await respuesta.Content.ReadFromJsonAsync<T>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (HttpRequestException) { Error = "No hay conexión con SatDte.Api. Inicia la API en el puerto 5175."; }
        catch (TaskCanceledException) { Error = "La API tardó demasiado en responder."; }
        catch (JsonException) { Error = "La API devolvió datos JSON inesperados."; }
        return default;
    }
}
