using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using RinhaBackend2025.Codigo;
using RinhaBackend2025.Codigo.Worker;
using StackExchange.Redis;

#region DI
var builder = WebApplication.CreateSlimBuilder(args);
builder.Configuration.AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true);

builder.Services.ConfigureHttpJsonOptions(options =>
{
	options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddSingleton(serviceProvider =>
{
	var configuration = serviceProvider.GetRequiredService<IConfiguration>();
	var connectionString = Environment.GetEnvironmentVariable("ConnectionString") ?? configuration.GetConnectionString("DefaultConfiguration") ?? throw new ApplicationException("Connection String não foi informada");
	return new SqlConnectionFactory(connectionString);
});
builder.Services.AddSingleton<IDatabase>(cfg =>
{
	var redisServer = Environment.GetEnvironmentVariable("RedisServer") ?? "localhost:6379";
	return ConnectionMultiplexer.Connect(redisServer).GetDatabase();
});
builder.Services.AddSingleton<PaymentRepository>();
builder.Services.AddSingleton<PaymentService>(serviceProvider =>
{
	var redis = serviceProvider.GetRequiredService<IDatabase>();
	var urlDefaultPaymentProcessor = Environment.GetEnvironmentVariable("PaymentProcessorDefaultUrl") ?? "http://localhost:8001";
	var urlFallbackPaymentProcessor = Environment.GetEnvironmentVariable("PaymentProcessorFallbackUrl") ?? "http://localhost:8002";

	var defaultPaymentProcessor = new PaymentProcessorClient(urlDefaultPaymentProcessor, 0.05m);
	var fallbackPaymentProcessor = new PaymentProcessorClient(urlFallbackPaymentProcessor, 0.15m);

	var paymentRepository = serviceProvider.GetRequiredService<PaymentRepository>();

	return new PaymentService(redis, defaultPaymentProcessor, fallbackPaymentProcessor, paymentRepository);
});


//if (Environment.GetEnvironmentVariable("UseWorker") == "true")
//{
	builder.Services.AddHostedService<WorkerPaymentBackgroundService>();
//}

var app = builder.Build();

#endregion

#region Endpoints
/// <summary>
/// Recebe um pagamento e coloca na fila para processamento.
/// </summary>
app.MapPost("/payments", static async ([FromBody] WorkerPaymentRequest paymentRequest, IDatabase redis, PaymentService paymentService) =>
{
	if (!await paymentService.TryAddPagamentoAsync(paymentRequest))
		return Results.UnprocessableEntity();

	return Results.Ok();
})
.DisableRequestTimeout();

/// <summary>
/// Retorna o resumo dos pagamentos processados.
/// </summary>
app.MapGet("/payments-summary", static async ([FromServices] PaymentService paymentService, DateTime? from, DateTime? to) =>
{
	var response = await paymentService.ObterResumoPagamentosAsync(from, to);
	return Results.Json(response);
})
.DisableRequestTimeout();

/// <summary>
/// Limpa os dados de pagamentos
/// </summary>
app.MapPost("/admin/purge-payments", static async (PaymentRepository paymentRepository) =>
{
	await paymentRepository.LimpezaDados();
	return Results.Ok();
});

await app.RunAsync();

#endregion

[JsonSerializable(typeof(PaymentSummaryResponse))]
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(HealthCheckResponse))]
[JsonSerializable(typeof(WorkerPaymentRequest))]
[JsonSerializable(typeof(PaymentProcessorRequest))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }
