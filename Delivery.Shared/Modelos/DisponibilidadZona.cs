using System;
using System.Collections.Generic;
using System.Text;

namespace Delivery.Shared.Modelos;

public record DisponibilidadZona
{
    public string Zona { get; set; }
    public int RepartidoresDisponibles { get; set; }
    public int RepartidoresOcupados { get; set; }
    public DateTime Momento { get; set; }
}