namespace Delivery.Api.Configuracion;

public class KafkaSettings
{
    public string BootstrapServers { get; set; }
    public string TopicPedidos { get; set; }
    public string TopicDisponibilidad { get; set; }
}