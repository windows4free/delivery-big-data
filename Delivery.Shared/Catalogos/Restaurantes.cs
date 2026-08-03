namespace Delivery.Shared.Catalogos;

public record RestauranteInfo(string Id, string Nombre, string Categoria, string ZonaCasera);

public static class Restaurantes
{
    public static readonly string[] Categorias =
    {
        "Comida rápida", "Italiana", "Sushi", "Mexicana", "Saludable", "Postres/Café"
    };

    // Nombre -> Categoría coherente
    private static readonly (string Nombre, string Categoria)[] CatalogoBase =
    {
        ("Burger Point",     "Comida rápida"),
        ("Pizza Nostra",     "Italiana"),
        ("Sushi Zen",        "Sushi"),
        ("Tacos El Primo",   "Mexicana"),
        ("Green Bowl",       "Saludable"),
        ("Café Dulce",       "Postres/Café"),
        ("Wingz House",      "Comida rápida"),
        ("Pasta Bella",      "Italiana"),
        ("Sakura Sushi",     "Sushi"),
        ("Taquería Central", "Mexicana"),
        ("Fit Kitchen",      "Saludable"),
        ("Choco Postres",    "Postres/Café"),
        ("Grill Express",    "Comida rápida"),
        ("Napoli Pizza",     "Italiana"),
        ("Roll & Rice",      "Sushi"),
        ("El Buen Taco",     "Mexicana"),
        ("Vida Sana",        "Saludable"),
        ("Panadería Luna",   "Postres/Café"),
        ("Broaster King",    "Comida rápida"),
        ("Trattoria Roma",   "Italiana"),
        ("Wok House",        "Sushi"),
        ("Fresh Salads",     "Saludable"),
        ("Café Aroma",       "Postres/Café"),
        ("Pollo Loco",       "Comida rápida"),
        ("Deli Gourmet",     "Mexicana"),
    };

    public static readonly List<RestauranteInfo> Catalogo = GenerarCatalogo();

    private static List<RestauranteInfo> GenerarCatalogo()
    {
        var random = new Random(42); // seed fijo, solo para asignar zona
        var zonas = Zonas.Todas;
        var lista = new List<RestauranteInfo>();

        for (int i = 0; i < CatalogoBase.Length; i++)
        {
            var (nombre, categoria) = CatalogoBase[i];
            var zona = zonas[random.Next(zonas.Length)];
            lista.Add(new RestauranteInfo($"R{i + 1:000}", nombre, categoria, zona));
        }

        return lista;
    }
}