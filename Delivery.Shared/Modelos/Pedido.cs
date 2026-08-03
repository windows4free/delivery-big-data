using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Delivery.Shared.Modelos;

public record Pedido
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? MongoId { get; set; }

    public string Id { get; set; } = Guid.NewGuid().ToString(); // Id del negocio (usado para detectar duplicados)
    public string Zona { get; set; }
    public string UsuarioId { get; set; }
    public string RestauranteId { get; set; }
    public string RestauranteNombre { get; set; }
    public string CategoriaComida { get; set; }
    public decimal Monto { get; set; }
    public int CantidadItems { get; set; }
    public string MetodoPago { get; set; }
    public DateTime Momento { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public double? PromocionAplicada { get; set; }
}