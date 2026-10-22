using Microsoft.AspNetCore.Mvc;
using SatDte.Core.Servicios;

namespace SatDte.Api.Controllers;

[ApiController]
[Route("api/sistema")]
public sealed class SistemaController : ControllerBase
{
    private readonly ServicioRtu _rtu;
    private readonly ServicioFacturas _facturas;

    public SistemaController(ServicioRtu rtu, ServicioFacturas facturas)
    {
        _rtu = rtu;
        _facturas = facturas;
    }

    [HttpGet("estado")]
    public IActionResult Estado() => Ok(new
    {
        estado = "activo",
        servicio = "SAT DTE - API",
        release = 4,
        contribuyentes = _rtu.Listar().Length,
        solicitudes = _facturas.Cantidad
    });

    [HttpPost("inicializar")]
    public IActionResult Inicializar()
    {
        _facturas.Reiniciar();
        _rtu.Reiniciar();
        return Ok(new { mensaje = "RTU y solicitudes de facturas inicializados correctamente." });
    }
}
