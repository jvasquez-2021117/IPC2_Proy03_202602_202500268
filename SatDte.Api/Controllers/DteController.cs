using Microsoft.AspNetCore.Mvc;
using SatDte.Core.Modelos;
using SatDte.Core.Servicios;

namespace SatDte.Api.Controllers;

[ApiController]
[Route("api/dte")]
public sealed class DteController : ControllerBase
{
    private readonly ServicioFacturas _servicio;
    public DteController(ServicioFacturas servicio) => _servicio = servicio;

    /// <summary>Recibe SIN modificaciones el arreglo de facturas del enunciado.</summary>
    [HttpPost("autorizar")]
    public ActionResult<RespuestaAutorizaciones> Autorizar([FromBody] List<FacturaEntrada?>? registros)
    {
        if (registros is null) return BadRequest(new { error = "Se requiere un arreglo JSON de facturas." });
        try { return Ok(_servicio.Procesar(registros)); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    /// <summary>Listado auxiliar para inspeccionar rechazos y aprobaciones (Release 2).</summary>
    [HttpGet("solicitudes")]
    public ActionResult<RegistroFactura[]> Solicitudes([FromQuery] int limite = 100)
        => Ok(_servicio.Listar(limite));
}
