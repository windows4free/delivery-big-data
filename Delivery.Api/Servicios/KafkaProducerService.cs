using Confluent.Kafka;
using Delivery.Api.Configuracion;
using Delivery.Shared.Modelos;
using System.Text.Json;

namespace Delivery.Api.Servicios;

public class KafkaProducerService : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(KafkaSettings settings, ILogger<KafkaProducerService> logger)
    {
        _settings = settings;
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            Acks = Acks.All,                  // espera confirmación de todas las réplicas (no perder datos)
            MessageSendMaxRetries = 3,
            EnableIdempotence = true          // evita duplicados por reintentos del propio producer
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task ProducirPedidoAsync(Pedido pedido)
    {
        var mensaje = new Message<string, string>
        {
            Key = pedido.Zona,   // clave = zona → decide la partición, mantiene orden por zona
            Value = JsonSerializer.Serialize(pedido)
        };

        await _producer.ProduceAsync(_settings.TopicPedidos, mensaje);
    }

    public async Task ProducirLoteAsync(IEnumerable<Pedido> pedidos)
    {
        var tareas = pedidos.Select(p => ProducirPedidoAsync(p));
        await Task.WhenAll(tareas);
        _producer.Flush(TimeSpan.FromSeconds(10)); // asegura que todo se envíe antes de continuar
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(5));
        _producer?.Dispose();
    }
}