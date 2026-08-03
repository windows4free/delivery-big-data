using System;
using System.Collections.Generic;
using System.Text;

namespace Delivery.Shared.Catalogos;

public static class Zonas
{
    public static readonly Dictionary<string, double> PesosPorZona = new()
    {
        { "Centro", 0.25 },
        { "Metropolitana", 0.20 },
        { "Norte", 0.18 },
        { "Sur", 0.15 },
        { "Este", 0.12 },
        { "Oeste", 0.10 }
    };

    public static readonly string[] Todas = PesosPorZona.Keys.ToArray();
}