using Confluent.Kafka;
using Delivery.Consumer.Configuracion;
using Delivery.Shared.Catalogos;
using Delivery.Shared.Interfaces;
using Delivery.Shared.Modelos;
using System.Text.Json;

namespace Delivery.Consumer;

public class KafkaConsumerService : BackgroundService
{
    private readonly string _instanciaId = Guid.NewGuid().ToString()[..8]; 
    private readonly KafkaSettings _settings;
    private readonly IPedidosRepository _pedidosRepo;
    private readonly ISaturacionRepository _saturacionRepo;
    private readonly ILogger<KafkaConsumerService> _logger;

    
    private readonly Dictionary<string, int> _repartidoresDisponibles;

    public KafkaConsumerService(
        KafkaSettings settings,
        IPedidosRepository pedidosRepo,
        ISaturacionRepository saturacionRepo,
        ILogger<KafkaConsumerService> logger)
    {
        _settings = settings;
        _pedidosRepo = pedidosRepo;
        _saturacionRepo = saturacionRepo;
        _logger = logger;

        
        var random = new Random(7);
        _repartidoresDisponibles = Zonas.Todas.ToDictionary(z => z, z => random.Next(10, 30));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = _settings.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_settings.TopicPedidos);

        _logger.LogInformation(
            "[Instancia {InstanciaId}] Consumer iniciado. Escuchando topic '{Topic}' como grupo '{Group}'",
            _instanciaId, _settings.TopicPedidos, _settings.GroupId);

        int procesados = 0, descartados = 0, duplicados = 0;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result;
                try
                {
                    result = consumer.Consume(TimeSpan.FromSeconds(1));
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consumiendo de Kafka");
                    continue;
                }

                if (result is null) continue;

                
                _logger.LogInformation(
    "[Instancia {InstanciaId}] Mensaje recibido -> Partición: {Particion} | Offset: {Offset} | Key(Zona): {Key}",
    _instanciaId, result.Partition.Value, result.Offset.Value, result.Message.Key);

                var pedido = LimpiarYDeserializar(result.Message.Value, ref descartados);
                if (pedido is null) continue;

                // Validar duplicado
                if (_pedidosRepo.ExisteDuplicado(pedido.Id))
                {
                    duplicados++;
                    _logger.LogWarning("Pedido duplicado descartado: {Id}", pedido.Id);
                    continue;
                }

                _pedidosRepo.GuardarPedido(pedido);
                procesados++;

                
                RecalcularSaturacion(pedido.Zona);

                if (procesados % 500 == 0)
                {
                    _logger.LogInformation(
                        "Progreso: {Procesados} procesados | {Descartados} descartados | {Duplicados} duplicados",
                        procesados, descartados, duplicados);
                }
            }
        }
        catch (OperationCanceledException)
        {
          
        }
        finally
        {
            _logger.LogInformation(
                "Consumer detenido. Total -> Procesados: {Procesados} | Descartados: {Descartados} | Duplicados: {Duplicados}",
                procesados, descartados, duplicados);
            consumer.Close();
        }
    }

    private Pedido? LimpiarYDeserializar(string json, ref int descartados)
    {
        try
        {
            var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var pedido = JsonSerializer.Deserialize<Pedido>(json, opciones);

            if (pedido is null)
            {
                descartados++;
                return null;
            }

           
            if (string.IsNullOrWhiteSpace(pedido.UsuarioId) ||
                string.IsNullOrWhiteSpace(pedido.RestauranteNombre))
            {
                _logger.LogWarning("Pedido descartado por campos nulos críticos: {Id}", pedido.Id);
                descartados++;
                return null;
            }

            
            if (!Zonas.Todas.Contains(pedido.Zona))
            {
                _logger.LogWarning("Pedido descartado por zona inválida: {Zona}", pedido.Zona);
                descartados++;
                return null;
            }

           
            if (pedido.Monto <= 0)
            {
                _logger.LogWarning("Pedido descartado por monto inválido: {Monto}", pedido.Monto);
                descartados++;
                return null;
            }

            return pedido;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("Mensaje descartado por JSON inválido: {Error}", ex.Message);
            descartados++;
            return null;
        }
    }

    private void RecalcularSaturacion(string zona)
    {
        var pedidosActivos = _pedidosRepo.ObtenerPedidosActivosPorZona(zona).Count();
        var repartidores = _repartidoresDisponibles.GetValueOrDefault(zona, 1);

        var saturacion = new SaturacionZona
        {
            Zona = zona,
            PedidosActivos = pedidosActivos,
            RepartidoresDisponibles = repartidores,
            IndiceSaturacion = repartidores > 0 ? Math.Round((double)pedidosActivos / repartidores, 2) : pedidosActivos,
            CalculadoEn = DateTime.UtcNow
        };

        _saturacionRepo.GuardarSaturacion(saturacion);
    }
}