using Microsoft.AspNetCore.Mvc;
using SatDte.Core.Modelos;
using SatDte.Core.Servicios;

namespace SatDte.Api.Controllers;

[ApiController]
[Route("api/rtu")]
public sealed class RtuController : ControllerBase
{
    private readonly ServicioRtu _servicio;

    public RtuController(ServicioRtu servicio) => _servicio = servicio;

    /// <summary>POST /api/rtu/importar — el cuerpo es el arreglo JSON del enunciado.</summary>
    [HttpPost("importar")]
    public ActionResult<RespuestaRtu> Importar([FromBody] List<ContribuyenteEntrada?>? registros)
    {
        if (registros is null)
            return BadRequest(new { error = "Se esperaba un arreglo JSON de contribuyentes." });

        try
        {
            return Ok(_servicio.Importar(registros));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>GET /api/rtu — listado completo (consulta auxiliar).</summary>
    [HttpGet]
    public ActionResult<Contribuyente[]> Listar() => Ok(_servicio.Listar());

    /// <summary>GET /api/rtu/7378106 — consulta auxiliar por NIT.</summary>
    [HttpGet("{nit}")]
    public ActionResult<Contribuyente> Buscar(string nit)
    {
        if (!_servicio.ValidarNit(nit))
            return BadRequest(new { error = "NIT inválido." });

        Contribuyente? contribuyente = _servicio.Buscar(nit);
        if (contribuyente is null)
            return NotFound(new { error = "NIT no encontrado en el RTU." });

        return Ok(contribuyente);
    }
}
