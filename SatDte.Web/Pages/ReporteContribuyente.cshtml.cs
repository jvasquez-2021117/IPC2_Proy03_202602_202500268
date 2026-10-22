using Microsoft.AspNetCore.Mvc;
using SatDte.Core.Modelos;

namespace SatDte.Web.Pages;

public sealed class ReporteContribuyenteModel : ConsultaApiPageModel
{
    public ReporteContribuyenteModel(IHttpClientFactory clientes) : base(clientes) { }

    [BindProperty(SupportsGet = true)] public string? Nit { get; set; }
    [BindProperty(SupportsGet = true)] public string Tipo { get; set; } = "emitidas";
    [BindProperty(SupportsGet = true)] public string? Inicio { get; set; }
    [BindProperty(SupportsGet = true)] public string? Fin { get; set; }
    public ReporteContribuyente? Reporte { get; private set; }

    public async Task OnGetAsync()
    {
        if (string.IsNullOrEmpty(Inicio) && string.IsNullOrEmpty(Fin) && string.IsNullOrWhiteSpace(Nit)) return;
        if (!ValidarFechas(Inicio, Fin)) return;
        if (string.IsNullOrWhiteSpace(Nit)) { Error = "Indica el NIT del contribuyente."; return; }
        if (Tipo != "emitidas" && Tipo != "recibidas") { Error = "Selecciona un tipo de factura."; return; }
        Reporte = await ObtenerAsync<ReporteContribuyente>(
            $"api/reportes/contribuyente?nit={Uri.EscapeDataString(Nit.Trim())}&tipo={Tipo}&inicio={Inicio}&fin={Fin}");
    }
    public async Task<IActionResult> OnGetPdfAsync()
    {
        if (!ValidarFechas(Inicio, Fin) || string.IsNullOrWhiteSpace(Nit) ||
            Tipo is not ("emitidas" or "recibidas"))
            return BadRequest("Filtros inválidos para descargar facturas por NIT.");
        string ruta = $"api/reportes/contribuyente/pdf?nit={Uri.EscapeDataString(Nit.Trim())}&tipo={Tipo}&inicio={Inicio}&fin={Fin}";
        return await DescargarPdfAsync(ruta, "facturas-contribuyente.pdf");
    }

}
