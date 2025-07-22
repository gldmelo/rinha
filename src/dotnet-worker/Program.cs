using System.Text.Json.Serialization;
using BackgroundWorker.Codigo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

#region DI
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<AppJsonSerializerContext>();

// Postgres
builder.Services.AddSingleton(serviceProvider =>
{
	var configuration = serviceProvider.GetRequiredService<IConfiguration>();
	var connectionString = Environment.GetEnvironmentVariable("ConnectionString") ?? configuration.GetConnectionString("DefaultConfiguration") ?? throw new ApplicationException("Connection String não foi informada");
	return new SqlConnectionFactory(connectionString);
});
// Redis
builder.Services.AddSingleton<IDatabase>(cfg =>
{
	var redisServer = Environment.GetEnvironmentVariable("RedisServer") ?? "localhost:6379";
	return ConnectionMultiplexer.Connect(redisServer).GetDatabase();
});
builder.Services.AddHostedService<PaymentWorker>();
builder.Services.AddSingleton<PaymentRepository>();
builder.Services.AddSingleton<PaymentService>(serviceProvider => 
{
	var urlDefaultPaymentProcessor = Environment.GetEnvironmentVariable("PaymentProcessorDefaultUrl") ?? "http://localhost:8001";
	var urlFallbackPaymentProcessor = Environment.GetEnvironmentVariable("PaymentProcessorFallbackUrl") ?? "http://localhost:8002";

	var defaultPaymentProcessor = new PaymentProcessorClient(urlDefaultPaymentProcessor, 0.05m);
	var fallbackPaymentProcessor = new PaymentProcessorClient(urlFallbackPaymentProcessor, 0.15m);
	var paymentRepository = serviceProvider.GetRequiredService<PaymentRepository>();
	
	return new PaymentService(defaultPaymentProcessor, fallbackPaymentProcessor, paymentRepository);
});

var host = builder.Build();
#endregion

await host.RunAsync();

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(HealthCheckResponse))]
[JsonSerializable(typeof(PaymentRequest))]
[JsonSerializable(typeof(PaymentProcessorRequest))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
