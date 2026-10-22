using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SatDte.Web.Pages;

public sealed class SolicitudesModel : PageModel
{
    private readonly IHttpClientFactory _clientes;
    public SolicitudesModel(IHttpClientFactory clientes) => _clientes = clientes;

    public List<SolicitudVista> Solicitudes { get; private set; } = new();
    public string? Error { get; private set; }

    public async Task OnGetAsync()
    {
        try
        {
            using HttpClient cliente = _clientes.CreateClient("SatApi");
            using HttpResponseMessage respuesta = await cliente.GetAsync("api/dte/solicitudes?limite=100");
            respuesta.EnsureSuccessStatusCode();
            List<SolicitudVista>? registros = await respuesta.Content.ReadFromJsonAsync<List<SolicitudVista>>(
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Solicitudes = registros ?? new();
        }
        catch (HttpRequestException) { Error = "No se pudo consultar la API. Confirma que SatDte.Api esté iniciado."; }
        catch (JsonException) { Error = "Respuesta inesperada de la API."; }
    }

    public sealed class SolicitudVista
    {
        public long NumeroSolicitud { get; set; }
        public string? Referencia { get; set; }
        public string? NitEmisor { get; set; }
        public string? NitReceptor { get; set; }
        public bool Aprobada { get; set; }
        public long? CodigoAprobacion { get; set; }
        public decimal? Total { get; set; }
        public string[] Errores { get; set; } = Array.Empty<string>();
    }
}
