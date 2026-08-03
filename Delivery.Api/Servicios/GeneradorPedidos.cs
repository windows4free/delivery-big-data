using Delivery.Shared.Catalogos;
using Delivery.Shared.Modelos;

namespace Delivery.Api.Servicios;

public class GeneradorPedidos
{
    private readonly Random _random = new();
    private readonly string[] _zonas = Zonas.Todas;
    private readonly double[] _pesosAcumulados;

    private static readonly string[] MetodosPago = { "Tarjeta", "Efectivo", "Billetera digital" };

    public GeneradorPedidos()
    {
        
        double acumulado = 0;
        _pesosAcumulados = new double[_zonas.Length];
        for (int i = 0; i < _zonas.Length; i++)
        {
            acumulado += Zonas.PesosPorZona[_zonas[i]];
            _pesosAcumulados[i] = acumulado;
        }
    }

    public Pedido GenerarPedido(bool horaPico = false)
    {
        var zona = SeleccionarZonaPonderada();
        var restaurante = SeleccionarRestauranteDeZona(zona);

        var pedido = new Pedido
        {
            Zona = zona,
            UsuarioId = $"U{_random.Next(1, 801):0000}",
            RestauranteId = restaurante.Id,
            RestauranteNombre = restaurante.Nombre,
            CategoriaComida = restaurante.Categoria,
            Monto = GenerarMonto(),
            CantidadItems = _random.Next(1, 8),
            MetodoPago = MetodosPago[_random.Next(MetodosPago.Length)],
            Momento = DateTime.UtcNow,
            Estado = "Pendiente",
            PromocionAplicada = _random.NextDouble() < 0.2 ? Math.Round(_random.Next(5, 30) / 100.0, 2) : null
        };

        
        return AplicarImperfecciones(pedido);
    }

    private string SeleccionarZonaPonderada()
    {
        double r = _random.NextDouble();
        for (int i = 0; i < _pesosAcumulados.Length; i++)
        {
            if (r <= _pesosAcumulados[i]) return _zonas[i];
        }
        return _zonas[^1];
    }

    private Delivery.Shared.Catalogos.RestauranteInfo SeleccionarRestauranteDeZona(string zona)
    {
        
        var candidatos = _random.NextDouble() < 0.8
            ? Restaurantes.Catalogo.Where(r => r.ZonaCasera == zona).ToList()
            : Restaurantes.Catalogo;

        if (candidatos.Count == 0) candidatos = Restaurantes.Catalogo;

        return candidatos[_random.Next(candidatos.Count)];
    }

    private decimal GenerarMonto()
    {
        
        double base_ = _random.NextDouble();
        double monto = base_ < 0.7
            ? 150 + _random.NextDouble() * 450   
            : 600 + _random.NextDouble() * 900;  

        return Math.Round((decimal)monto, 2);
    }

    private Pedido AplicarImperfecciones(Pedido pedido)
    {
        double r = _random.NextDouble();

        if (r < 0.015) 
        {
            return pedido with { Monto = -pedido.Monto };
        }
        if (r < 0.03) 
        {
            return pedido with { Zona = "ZonaDesconocida99" };
        }
        if (r < 0.045) 
        {
            return pedido with { UsuarioId = null, RestauranteNombre = null };
        }
        

        return pedido;
    }

    public List<Pedido> GenerarLote(int cantidad, bool horaPico = false)
    {
        var lote = new List<Pedido>();
        for (int i = 0; i < cantidad; i++)
        {
            lote.Add(GenerarPedido(horaPico));
        }

        
        int duplicados = (int)(cantidad * 0.005);
        for (int i = 0; i < duplicados && lote.Count > 0; i++)
        {
            var original = lote[_random.Next(lote.Count)];
            lote.Add(original with { }); 
        }

        return lote;
    }

    public bool EsHoraPico()
    {
        var hora = DateTime.Now.Hour;
        return (hora >= 12 && hora < 14) || (hora >= 19 && hora < 21);
    }
}