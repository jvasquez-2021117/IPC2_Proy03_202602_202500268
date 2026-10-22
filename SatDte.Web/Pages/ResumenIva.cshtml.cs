using Microsoft.AspNetCore.Mvc;
using SatDte.Core.Modelos;

namespace SatDte.Web.Pages;

public sealed class ResumenIvaModel : ConsultaApiPageModel
{
    public ResumenIvaModel(IHttpClientFactory clientes) : base(clientes) { }
    [BindProperty(SupportsGet = true)] public string? Fecha { get; set; }
    public ResumenIvaFecha? Reporte { get; private set; }

    public async Task OnGetAsync()
    {
        if (string.IsNullOrEmpty(Fecha)) return;
        if (!FechaValida(Fecha)) { Error = "Introduce una fecha válida."; return; }
        Reporte = await ObtenerAsync<ResumenIvaFecha>("api/reportes/iva?fecha=" + Fecha);
    }
    public async Task<IActionResult> OnGetPdfAsync()
    {
        if (!FechaValida(Fecha)) return BadRequest("Fecha inválida para descargar el resumen de IVA.");
        return await DescargarPdfAsync("api/reportes/iva/pdf?fecha=" + Fecha, "resumen-iva.pdf");
    }

}
