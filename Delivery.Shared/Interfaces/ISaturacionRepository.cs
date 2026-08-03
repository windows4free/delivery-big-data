using System;
using System.Collections.Generic;
using System.Text;

using Delivery.Shared.Modelos;

namespace Delivery.Shared.Interfaces;

public interface ISaturacionRepository
{
    void GuardarSaturacion(SaturacionZona saturacion);
    SaturacionZona? ObtenerSaturacionActual(string zona);
    IEnumerable<SaturacionZona> ObtenerHistorial(string zona, int minutos);
    IEnumerable<SaturacionZona> ObtenerSaturacionTodasLasZonas();
}