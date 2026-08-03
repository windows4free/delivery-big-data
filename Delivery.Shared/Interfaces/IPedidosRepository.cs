using Delivery.Shared.Modelos;

namespace Delivery.Shared.Interfaces;

public interface IPedidosRepository
{
    void GuardarPedido(Pedido pedido);
    bool ExisteDuplicado(string idNegocio);
    IEnumerable<Pedido> ObtenerPedidosActivosPorZona(string zona);
    IEnumerable<Pedido> ObtenerTodos();
}