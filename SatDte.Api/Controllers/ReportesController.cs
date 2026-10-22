using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SatDte.Core.Servicios;
using SatDte.Core.Exportacion;

namespace SatDte.Api.Controllers;

/// <summary>Consultas independientes consumibles desde Razor Pages o Postman.</summary>
[ApiController]
[Route("api/reportes")]
public sealed class ReportesController : ControllerBase
{
    private readonly ServicioReportes _reportes;
    private readonly GeneradorReportesPdf _pdf;
    public ReportesController(ServicioReportes reportes, GeneradorReportesPdf pdf)
    {
        _reportes = reportes;
        _pdf = pdf;
    }

    [HttpGet("contribuyente")]
    public IActionResult Contribuyente([FromQuery] string? nit, [FromQuery] string? tipo,
        [FromQuery] string? inicio, [FromQuery] string? fin)
    {
        if (!IntentarRango(inicio, fin, out DateOnly desde, out DateOnly hasta))
            return BadRequest(new { error = "Fechas inválidas. Usa yyyy-MM-dd y fecha inicial <= final." });
        if (!string.Equals(tipo, "emitidas", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(tipo, "recibidas", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "El tipo debe ser emitidas o recibidas." });
        if (string.IsNullOrWhiteSpace(nit)) return BadRequest(new { error = "Debes proporcionar un NIT." });
        try
        {
            return Ok(_reportes.ConsultarContribuyente(nit.Trim(),
                string.Equals(tipo, "emitidas", StringComparison.OrdinalIgnoreCase), desde, hasta));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpGet("iva")]
    public IActionResult Iva([FromQuery] string? fecha)
    {
        if (!IntentarFecha(fecha, out DateOnly dia))
            return BadRequest(new { error = "Fecha inválida. Usa yyyy-MM-dd." });
        return Ok(_reportes.ConsultarIva(dia));
    }

    [HttpGet("rango")]
    public IActionResult Rango([FromQuery] string? inicio, [FromQuery] string? fin,
        [FromQuery] string? modo = "total")
    {
        if (!IntentarRango(inicio, fin, out DateOnly desde, out DateOnly hasta))
            return BadRequest(new { error = "Fechas inválidas. Usa yyyy-MM-dd y fecha inicial <= final." });
        if (!string.Equals(modo, "total", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(modo, "siniva", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "El modo debe ser total o siniva." });
        return Ok(_reportes.ConsultarRango(desde, hasta,
            string.Equals(modo, "siniva", StringComparison.OrdinalIgnoreCase)));
    }

    [HttpGet("recepciones")]
    public IActionResult Recepciones([FromQuery] string? inicio, [FromQuery] string? fin)
    {
        if (!IntentarRango(inicio, fin, out DateOnly desde, out DateOnly hasta))
            return BadRequest(new { error = "Fechas inválidas. Usa yyyy-MM-dd y fecha inicial <= final." });
        return Ok(_reportes.ConsultarRecepciones(desde, hasta));
    }

    // Exportaciones HTTP independientes de Razor Pages: también se prueban desde Postman.
    [HttpGet("contribuyente/pdf")]
    public IActionResult ContribuyentePdf([FromQuery] string? nit, [FromQuery] string? tipo,
        [FromQuery] string? inicio, [FromQuery] string? fin)
    {
        if (!IntentarRango(inicio, fin, out DateOnly desde, out DateOnly hasta))
            return BadRequest(new { error = "Fechas inválidas. Usa yyyy-MM-dd y fecha inicial <= final." });
        if (tipo is not ("emitidas" or "recibidas") || string.IsNullOrWhiteSpace(nit))
            return BadRequest(new { error = "Especifica NIT y tipo emitidas o recibidas." });
        try
        {
            var reporte = _reportes.ConsultarContribuyente(nit.Trim(), tipo == "emitidas", desde, hasta);
            return File(_pdf.Contribuyente(reporte), "application/pdf", "facturas-contribuyente.pdf");
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpGet("iva/pdf")]
    public IActionResult IvaPdf([FromQuery] string? fecha)
    {
        if (!IntentarFecha(fecha, out DateOnly dia))
            return BadRequest(new { error = "Fecha inválida. Usa yyyy-MM-dd." });
        return File(_pdf.Iva(_reportes.ConsultarIva(dia)), "application/pdf", "resumen-iva.pdf");
    }

    [HttpGet("rango/pdf")]
    public IActionResult RangoPdf([FromQuery] string? inicio, [FromQuery] string? fin,
        [FromQuery] string? modo = "total")
    {
        if (!IntentarRango(inicio, fin, out DateOnly desde, out DateOnly hasta))
            return BadRequest(new { error = "Fechas inválidas. Usa yyyy-MM-dd y fecha inicial <= final." });
        if (modo is not ("total" or "siniva"))
            return BadRequest(new { error = "El modo debe ser total o siniva." });
        return File(_pdf.Rango(_reportes.ConsultarRango(desde, hasta, modo == "siniva")),
            "application/pdf", "resumen-fechas.pdf");
    }

    [HttpGet("recepciones/pdf")]
    public IActionResult RecepcionesPdf([FromQuery] string? inicio, [FromQuery] string? fin)
    {
        if (!IntentarRango(inicio, fin, out DateOnly desde, out DateOnly hasta))
            return BadRequest(new { error = "Fechas inválidas. Usa yyyy-MM-dd y fecha inicial <= final." });
        return File(_pdf.Recepciones(_reportes.ConsultarRecepciones(desde, hasta)),
            "application/pdf", "reporte-recepciones.pdf");
    }

    private static bool IntentarFecha(string? texto, out DateOnly fecha) =>
        DateOnly.TryParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out fecha);

    private static bool IntentarRango(string? inicio, string? fin, out DateOnly desde, out DateOnly hasta)
    {
        bool inicioValido = IntentarFecha(inicio, out desde);
        bool finValido = IntentarFecha(fin, out hasta);
        return inicioValido && finValido && desde <= hasta;
    }
}
