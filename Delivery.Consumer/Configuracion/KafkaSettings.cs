namespace Delivery.Consumer.Configuracion;

public class KafkaSettings
{
    public string BootstrapServers { get; set; }
    public string TopicPedidos { get; set; }
    public string GroupId { get; set; }
}