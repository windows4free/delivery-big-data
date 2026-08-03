using Delivery.Api.Servicios;
using Delivery.Shared.Modelos;
using Microsoft.AspNetCore.Mvc;

namespace Delivery.Api.Controllers;

[ApiController]
[Route("api/pedidos")]
public class PedidosController : ControllerBase
{
    private readonly GeneradorPedidos _generador;
    private readonly KafkaProducerService _productor;
    private readonly ILogger<PedidosController> _logger;

    public PedidosController(GeneradorPedidos generador, KafkaProducerService productor, ILogger<PedidosController> logger)
    {
        _generador = generador;
        _productor = productor;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> GenerarUno()
    {
        var horaPico = _generador.EsHoraPico();
        var pedido = _generador.GenerarPedido(horaPico);

        await _productor.ProducirPedidoAsync(pedido);

        _logger.LogInformation("Pedido individual generado: {Zona} - {Restaurante}", pedido.Zona, pedido.RestauranteNombre);

        return Ok(pedido);
    }

    [HttpPost("lote")]
    public async Task<IActionResult> GenerarLote([FromQuery] int cantidad)
    {
        if (cantidad <= 0 || cantidad > 50000)
            return BadRequest("La cantidad debe estar entre 1 y 50,000.");

        var horaPico = _generador.EsHoraPico();
        var inicio = DateTime.UtcNow;

        var lote = _generador.GenerarLote(cantidad, horaPico);
        await _productor.ProducirLoteAsync(lote);

        var duracion = (DateTime.UtcNow - inicio).TotalSeconds;
        var throughput = cantidad / duracion;

        _logger.LogInformation(
            "Lote de {Cantidad} pedidos enviado en {Duracion:F2}s ({Throughput:F0} pedidos/seg)",
            cantidad, duracion, throughput);

        return Ok(new
        {
            cantidadEnviada = cantidad,
            duracionSegundos = Math.Round(duracion, 2),
            throughputPorSegundo = Math.Round(throughput, 0),
            horaPicoDetectada = horaPico
        });
    }

    [HttpGet("hora-pico")]
    public IActionResult VerificarHoraPico()
    {
        return Ok(new { esHoraPico = _generador.EsHoraPico(), horaActual = DateTime.Now.ToString("HH:mm") });
    }
}