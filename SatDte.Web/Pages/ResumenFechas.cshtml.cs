using Microsoft.AspNetCore.Mvc;
using SatDte.Core.Modelos;

namespace SatDte.Web.Pages;

public sealed class ResumenFechasModel : ConsultaApiPageModel
{
    public ResumenFechasModel(IHttpClientFactory clientes) : base(clientes) { }
    [BindProperty(SupportsGet = true)] public string? Inicio { get; set; }
    [BindProperty(SupportsGet = true)] public string? Fin { get; set; }
    [BindProperty(SupportsGet = true)] public string Modo { get; set; } = "total";
    public ResumenRangoFechas? Reporte { get; private set; }

    public async Task OnGetAsync()
    {
        if (string.IsNullOrEmpty(Inicio) && string.IsNullOrEmpty(Fin)) return;
        if (!ValidarFechas(Inicio, Fin)) return;
        if (Modo != "total" && Modo != "siniva") { Error = "Selecciona total o sin IVA."; return; }
        Reporte = await ObtenerAsync<ResumenRangoFechas>(
            $"api/reportes/rango?inicio={Inicio}&fin={Fin}&modo={Modo}");
    }
}
