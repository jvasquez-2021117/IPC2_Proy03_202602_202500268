using Microsoft.AspNetCore.Mvc;
using SatDte.Core.Modelos;

namespace SatDte.Web.Pages;

public sealed class ReporteRecepcionesModel : ConsultaApiPageModel
{
    public ReporteRecepcionesModel(IHttpClientFactory clientes) : base(clientes) { }
    [BindProperty(SupportsGet = true)] public string? Inicio { get; set; }
    [BindProperty(SupportsGet = true)] public string? Fin { get; set; }
    public ReporteRecepciones? Reporte { get; private set; }

    public async Task OnGetAsync()
    {
        if (string.IsNullOrEmpty(Inicio) && string.IsNullOrEmpty(Fin)) return;
        if (!ValidarFechas(Inicio, Fin)) return;
        Reporte = await ObtenerAsync<ReporteRecepciones>(
            $"api/reportes/recepciones?inicio={Inicio}&fin={Fin}");
    }
}
