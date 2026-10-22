using Microsoft.AspNetCore.Mvc;
using SatDte.Core.Servicios;

namespace SatDte.Api.Controllers;

[ApiController]
[Route("api/sistema")]
public sealed class SistemaController : ControllerBase
{
    private readonly ServicioRtu _servicio;

    public SistemaController(ServicioRtu servicio) => _servicio = servicio;

    [HttpGet("estado")]
    public IActionResult Estado() => Ok(new
    {
        estado = "activo",
        servicio = "SAT DTE - API",
        release = 1,
        contribuyentes = _servicio.Listar().Length
    });

    [HttpPost("inicializar")]
    public IActionResult Inicializar()
    {
        _servicio.Reiniciar();
        return Ok(new { mensaje = "Datos del sistema inicializados correctamente." });
    }
}
