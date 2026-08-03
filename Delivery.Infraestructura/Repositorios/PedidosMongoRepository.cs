using Delivery.Shared.Interfaces;
using Delivery.Shared.Modelos;
using MongoDB.Driver;

namespace Delivery.Infraestructura.Repositorios;

public class PedidosMongoRepository : IPedidosRepository
{
    private readonly IMongoCollection<Pedido> _coleccion;

    public PedidosMongoRepository(IMongoClient client, string databaseName)
    {
        var db = client.GetDatabase(databaseName);
        _coleccion = db.GetCollection<Pedido>("Pedidos");
    }

    public bool ExisteDuplicado(string idNegocio)
    {
        return _coleccion.Find(p => p.Id == idNegocio).Any();
    }

    public void GuardarPedido(Pedido pedido)
    {
        _coleccion.InsertOne(pedido);
    }

    public IEnumerable<Pedido> ObtenerPedidosActivosPorZona(string zona)
    {
        var estadosActivos = new[] { "Pendiente", "En preparación", "En camino" };
        return _coleccion.Find(p => p.Zona == zona && estadosActivos.Contains(p.Estado)).ToList();
    }

    public IEnumerable<Pedido> ObtenerTodos()
    {
        return _coleccion.Find(FilterDefinition<Pedido>.Empty).ToList();
    }
}