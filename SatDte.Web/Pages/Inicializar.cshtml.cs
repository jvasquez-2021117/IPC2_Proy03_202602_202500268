using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SatDte.Web.Pages;

public sealed class InicializarModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    public InicializarModel(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public string? Mensaje { get; private set; }
    public string? Error { get; private set; }

    public async Task OnPostAsync()
    {
        try
        {
            using HttpClient cliente = _httpClientFactory.CreateClient("SatApi");
            using HttpResponseMessage respuesta = await cliente.PostAsync("api/sistema/inicializar", content: null);
            if (!respuesta.IsSuccessStatusCode)
            {
                Error = $"La API devolvió {(int)respuesta.StatusCode}: " + await respuesta.Content.ReadAsStringAsync();
                return;
            }
            Mensaje = "El RTU se inicializó correctamente. Puedes consultar la lista para comprobar que está vacía.";
        }
        catch (HttpRequestException)
        {
            Error = "No se pudo conectar con la API. Comprueba el proyecto SatDte.Api.";
        }
        catch (TaskCanceledException)
        {
            Error = "La API no respondió dentro del tiempo permitido.";
        }
    }
}
