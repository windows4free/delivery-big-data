using Delivery.Repartidores;
using Delivery.Repartidores.Configuracion;
using Delivery.Infraestructura.Repositorios;
using Delivery.Shared.Interfaces;
using MongoDB.Driver;

var builder = Host.CreateApplicationBuilder(args);

var mongoConnectionString = builder.Configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
var mongoDatabase = builder.Configuration["Mongo:Database"] ?? "DeliveryBigData";
var mongoClient = new MongoClient(mongoConnectionString);

var repartidoresSettings = new RepartidoresSettings
{
    IntervaloSegundos = int.Parse(builder.Configuration["Repartidores:IntervaloSegundos"] ?? "15")
};

builder.Services.AddSingleton<IMongoClient>(mongoClient);
builder.Services.AddSingleton<ISaturacionRepository>(new SaturacionMongoRepository(mongoClient, mongoDatabase));
builder.Services.AddSingleton(repartidoresSettings);
builder.Services.AddHostedService<RepartidoresService>();

var host = builder.Build();
host.Run();