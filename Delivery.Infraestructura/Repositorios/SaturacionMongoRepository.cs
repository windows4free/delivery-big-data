using Delivery.Shared.Interfaces;
using Delivery.Shared.Modelos;
using MongoDB.Driver;

namespace Delivery.Infraestructura.Repositorios;

public class SaturacionMongoRepository : ISaturacionRepository
{
    private readonly IMongoCollection<SaturacionZona> _coleccion;

    public SaturacionMongoRepository(IMongoClient client, string databaseName)
    {
        var db = client.GetDatabase(databaseName);
        _coleccion = db.GetCollection<SaturacionZona>("Saturacion");
    }

    public void GuardarSaturacion(SaturacionZona saturacion)
    {
        _coleccion.InsertOne(saturacion);
    }

    public SaturacionZona? ObtenerSaturacionActual(string zona)
    {
        return _coleccion
            .Find(s => s.Zona == zona)
            .SortByDescending(s => s.CalculadoEn)
            .FirstOrDefault();
    }

    public IEnumerable<SaturacionZona> ObtenerHistorial(string zona, int minutos)
    {
        var desde = DateTime.UtcNow.AddMinutes(-minutos);
        return _coleccion
            .Find(s => s.Zona == zona && s.CalculadoEn >= desde)
            .SortBy(s => s.CalculadoEn)
            .ToList();
    }

    public IEnumerable<SaturacionZona> ObtenerSaturacionTodasLasZonas()
    {
        return Delivery.Shared.Catalogos.Zonas.Todas
            .Select(zona => ObtenerSaturacionActual(zona))
            .Where(s => s != null)
            .Select(s => s!)
            .ToList();
    }
}