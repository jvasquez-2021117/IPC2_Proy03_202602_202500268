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
    public async Task<IActionResult> OnGetPdfAsync()
    {
        if (!ValidarFechas(Inicio, Fin)) return BadRequest("Rango inválido para descargar el reporte.");
        return await DescargarPdfAsync($"api/reportes/recepciones/pdf?inicio={Inicio}&fin={Fin}", "reporte-recepciones.pdf");
    }

}
