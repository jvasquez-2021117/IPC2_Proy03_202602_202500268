using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SatDte.Web.Pages;

public sealed class ContribuyentesModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    public ContribuyentesModel(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public ContribuyenteFila[] Contribuyentes { get; private set; } = Array.Empty<ContribuyenteFila>();
    public string? Error { get; private set; }

    public async Task OnGetAsync()
    {
        try
        {
            using HttpClient cliente = _httpClientFactory.CreateClient("SatApi");
            Contribuyentes = await cliente.GetFromJsonAsync<ContribuyenteFila[]>("api/rtu")
                            ?? Array.Empty<ContribuyenteFila>();
        }
        catch (HttpRequestException)
        {
            Error = "No se puede acceder a la API. Inicia SatDte.Api (puerto 5175) y recarga esta página.";
        }
        catch (TaskCanceledException)
        {
            Error = "Se agotó el tiempo de espera del servidor de API.";
        }
    }

    public sealed class ContribuyenteFila
    {
        [JsonPropertyName("NIT")]
        public string Nit { get; set; } = string.Empty;

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;
    }
}
