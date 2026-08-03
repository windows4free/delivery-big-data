using Delivery.Consumer;
using Delivery.Consumer.Configuracion;
using Delivery.Infraestructura.Repositorios;
using Delivery.Shared.Interfaces;
using MongoDB.Driver;

var builder = Host.CreateApplicationBuilder(args);

var kafkaSettings = new KafkaSettings
{
    BootstrapServers = builder.Configuration["Kafka:BootstrapServers" ?? "localhost:9092",
    TopicPedidos = builder.Configuration["Kafka:TopicPedidos"] ?? "pedidos",
    GroupId = builder.Configuration["Kafka:GroupId"] ?? "delivery-consumer-group"
};

var mongoConnectionString = builder.Configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
var mongoDatabase = builder.Configuration["Mongo:Database"] ?? "DeliveryBigData";
var mongoClient = new MongoClient(mongoConnectionString);

builder.Services.AddSingleton<IMongoClient>(mongoClient);
builder.Services.AddSingleton<IPedidosRepository>(new PedidosMongoRepository(mongoClient, mongoDatabase));
builder.Services.AddSingleton<ISaturacionRepository>(new SaturacionMongoRepository(mongoClient, mongoDatabase));
builder.Services.AddSingleton(kafkaSettings);
builder.Services.AddHostedService<KafkaConsumerService>();

var host = builder.Build();
host.Run();