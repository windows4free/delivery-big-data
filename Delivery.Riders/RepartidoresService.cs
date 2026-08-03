using Delivery.Repartidores.Configuracion;
using Delivery.Shared.Catalogos;
using Delivery.Shared.Interfaces;
using Delivery.Shared.Modelos;
using MongoDB.Driver;

namespace Delivery.Repartidores;

public class RepartidoresService : BackgroundService
{
    private readonly ISaturacionRepository _saturacionRepo;
    private readonly IMongoCollection<Pedido> _pedidosCollection;
    private readonly RepartidoresSettings _settings;
    private readonly ILogger<RepartidoresService> _logger;
    private readonly Random _random = new();

    // Estado en memoria de repartidores por zona
    private readonly Dictionary<string, int> _disponibles;
    private readonly Dictionary<string, int> _ocupados;

    private static readonly string[] EstadosProgresion =
    {
        "Pendiente", "En preparación", "En camino", "Entregado"
    };

    public RepartidoresService(
        ISaturacionRepository saturacionRepo,
        IMongoClient mongoClient,
        RepartidoresSettings settings,
        ILogger<RepartidoresService> logger)
    {
        _saturacionRepo = saturacionRepo;
        _settings = settings;
        _logger = logger;

        var db = mongoClient.GetDatabase("DeliveryBigData");
        _pedidosCollection = db.GetCollection<Pedido>("Pedidos");

        // Inicializar repartidores base por zona
        _disponibles = Zonas.Todas.ToDictionary(z => z, z => _random.Next(15, 30));
        _ocupados = Zonas.Todas.ToDictionary(z => z, z => 0);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de repartidores iniciado. Intervalo: {Intervalo}s", _settings.IntervaloSegundos);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ActualizarDisponibilidadRepartidores();
                await AvanzarEstadosPedidosAsync();
                RecalcularSaturacionTodasLasZonas();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ciclo de repartidores");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.IntervaloSegundos), stoppingToken);
        }
    }

    private void ActualizarDisponibilidadRepartidores()
    {
        foreach (var zona in Zonas.Todas)
        {
            var delta = _random.Next(-3, 4);
            // Límite inferior 2, límite superior 40 (techo realista)
            _disponibles[zona] = Math.Clamp(_disponibles[zona] + delta, 2, 40);
        }

        _logger.LogInformation("Disponibilidad actualizada: {Detalle}",
            string.Join(", ", _disponibles.Select(kv => $"{kv.Key}={kv.Value}")));
    }

    private async Task AvanzarEstadosPedidosAsync()
    {
        var filtro = Builders<Pedido>.Filter.Ne(p => p.Estado, "Entregado");
        var pedidosActivos = await _pedidosCollection.Find(filtro).ToListAsync();

        int avanzados = 0;

        foreach (var pedido in pedidosActivos)
        {
            // 30% de probabilidad de avanzar de estado en cada ciclo
            if (_random.NextDouble() < 0.30)
            {
                var indiceActual = Array.IndexOf(EstadosProgresion, pedido.Estado);
                if (indiceActual >= 0 && indiceActual < EstadosProgresion.Length - 1)
                {
                    var nuevoEstado = EstadosProgresion[indiceActual + 1];
                    var update = Builders<Pedido>.Update.Set(p => p.Estado, nuevoEstado);
                    var filtroUno = Builders<Pedido>.Filter.Eq(p => p.MongoId, pedido.MongoId);

                    await _pedidosCollection.UpdateOneAsync(filtroUno, update);
                    avanzados++;

                    if (nuevoEstado == "Entregado")
                    {
                        _ocupados[pedido.Zona] = Math.Max(0, _ocupados[pedido.Zona] - 1);
                        _disponibles[pedido.Zona] = Math.Min(40, _disponibles[pedido.Zona] + 1); // Tope aquí también
                    }
                    else if (indiceActual == 0) // Pendiente -> En preparación: asigna repartidor
                    {
                        if (_disponibles[pedido.Zona] > 0)
                        {
                            _disponibles[pedido.Zona]--;
                            _ocupados[pedido.Zona]++;
                        }
                    }
                }
            }
        }

        _logger.LogInformation("Pedidos avanzados de estado: {Avanzados}", avanzados);
    }

    private void RecalcularSaturacionTodasLasZonas()
    {
        var estadosActivos = new[] { "Pendiente", "En preparación", "En camino" };

        foreach (var zona in Zonas.Todas)
        {
            var filtro = Builders<Pedido>.Filter.And(
                Builders<Pedido>.Filter.Eq(p => p.Zona, zona),
                Builders<Pedido>.Filter.In(p => p.Estado, estadosActivos)
            );

            var pedidosActivos = (int)_pedidosCollection.CountDocuments(filtro);
            var repartidoresDisponibles = _disponibles[zona];

            var saturacion = new SaturacionZona
            {
                Zona = zona,
                PedidosActivos = pedidosActivos,
                RepartidoresDisponibles = repartidoresDisponibles,
                IndiceSaturacion = repartidoresDisponibles > 0
                    ? Math.Round((double)pedidosActivos / repartidoresDisponibles, 2)
                    : pedidosActivos,
                CalculadoEn = DateTime.UtcNow
            };

            _saturacionRepo.GuardarSaturacion(saturacion);
        }

        _logger.LogInformation("Saturación recalculada para las 6 zonas");
    }
}