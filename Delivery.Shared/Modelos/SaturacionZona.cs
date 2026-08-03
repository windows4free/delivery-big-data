using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Delivery.Shared.Modelos;

public record SaturacionZona
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string Zona { get; set; }
    public int PedidosActivos { get; set; }
    public int RepartidoresDisponibles { get; set; }
    public double IndiceSaturacion { get; set; }
    public DateTime CalculadoEn { get; set; }
}