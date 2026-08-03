using Delivery.Api.Configuracion;
using Delivery.Api.Servicios;
using Delivery.Infraestructura.Repositorios;
using Delivery.Shared.Interfaces;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var kafkaSettings = new KafkaSettings
{
    BootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
    TopicPedidos = builder.Configuration["Kafka:TopicPedidos"] ?? "pedidos",
    TopicDisponibilidad = builder.Configuration["Kafka:TopicDisponibilidad"] ?? "disponibilidad-repartidores"
};

var mongoConnectionString = builder.Configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
var mongoDatabase = builder.Configuration["Mongo:Database"] ?? "DeliveryBigData";
var mongoClient = new MongoClient(mongoConnectionString);

builder.Services.AddSingleton<IMongoClient>(mongoClient);
builder.Services.AddSingleton<IPedidosRepository>(new PedidosMongoRepository(mongoClient, mongoDatabase));
builder.Services.AddSingleton<ISaturacionRepository>(new SaturacionMongoRepository(mongoClient, mongoDatabase));
builder.Services.AddSingleton(kafkaSettings);
builder.Services.AddSingleton<GeneradorPedidos>();
builder.Services.AddSingleton<KafkaProducerService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirReact", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("PermitirReact");
app.UseAuthorization();
app.MapControllers();

app.Run();