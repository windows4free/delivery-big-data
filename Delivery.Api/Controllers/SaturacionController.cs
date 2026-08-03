using Delivery.Shared.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Delivery.Api.Controllers;

[ApiController]
[Route("api/saturacion")]
public class SaturacionController : ControllerBase
{
    private readonly ISaturacionRepository _saturacionRepo;

    public SaturacionController(ISaturacionRepository saturacionRepo)
    {
        _saturacionRepo = saturacionRepo;
    }

    [HttpGet]
    public IActionResult ObtenerTodasLasZonas()
    {
        return Ok(_saturacionRepo.ObtenerSaturacionTodasLasZonas());
    }

    [HttpGet("{zona}")]
    public IActionResult ObtenerPorZona(string zona)
    {
        var actual = _saturacionRepo.ObtenerSaturacionActual(zona);
        if (actual is null) return NotFound();
        return Ok(actual);
    }

    [HttpGet("{zona}/historial")]
    public IActionResult ObtenerHistorial(string zona, [FromQuery] int minutos = 30)
    {
        return Ok(_saturacionRepo.ObtenerHistorial(zona, minutos));
    }
}