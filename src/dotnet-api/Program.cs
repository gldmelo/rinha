using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.RateLimiting;
using RinhaBackend2025.Codigo;
using RinhaBackend2025.Codigo.Worker;
using StackExchange.Redis;

const string redisFila = "rinha";

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
builder.Services.AddScoped<ApiPaymentRepository>();
builder.Services.AddScoped<PaymentService>();

builder.Services.AddSingleton<WorkerPaymentRepository>();
builder.Services.AddSingleton<WorkerPaymentService>(serviceProvider =>
{
	var urlDefaultPaymentProcessor = Environment.GetEnvironmentVariable("PaymentProcessorDefaultUrl") ?? "http://localhost:8001";
	var urlFallbackPaymentProcessor = Environment.GetEnvironmentVariable("PaymentProcessorFallbackUrl") ?? "http://localhost:8002";

	var defaultPaymentProcessor = new PaymentProcessorClient(urlDefaultPaymentProcessor, 0.05m);
	var fallbackPaymentProcessor = new PaymentProcessorClient(urlFallbackPaymentProcessor, 0.15m);
	var paymentRepository = serviceProvider.GetRequiredService<WorkerPaymentRepository>();

	return new WorkerPaymentService(defaultPaymentProcessor, fallbackPaymentProcessor, paymentRepository);
});

builder.Services.AddSingleton<IDatabase>(cfg =>
{
	var redisServer = Environment.GetEnvironmentVariable("RedisServer") ?? "localhost:6379";
	return ConnectionMultiplexer.Connect(redisServer).GetDatabase();
});

builder.Services.AddHostedService<WorkerPaymentBackgroundService>();

//var slidingPolicy = "concurrency";
//builder.Services.AddRateLimiter(o =>
//{
//	o.RejectionStatusCode = 429;
//	o.AddConcurrencyLimiter(policyName: slidingPolicy, options =>
//	{
//		options.PermitLimit = 1;
//		options.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
//		options.QueueLimit = 30000;
//	});
//});

var app = builder.Build();
//app.UseRateLimiter();

#endregion

#region Endpoints
/// <summary>
/// Recebe um pagamento e coloca na fila para processamento.
/// </summary>
app.MapPost("/payments", static async (HttpRequest request, IDatabase redis) =>
{
	using var reader = new StreamReader(request.Body);
	var rawBody = await reader.ReadToEndAsync();

	// Coloca o pagamento na fila do Redis e retorna OK o mais rápido possível
	await redis.ListRightPushAsync(redisFila, rawBody);
	return Results.Ok();
});
//.RequireRateLimiting(slidingPolicy)
//.DisableRequestTimeout();

/// <summary>
/// Retorna o resumo dos pagamentos processados.
/// </summary>
app.MapGet("/payments-summary", static async ([FromServices] PaymentService paymentService, DateTime? from, DateTime? to) =>
{
	var response = await paymentService.ObterResumoPagamentosAsync(from, to);
	//Thread.Sleep(1000); // Simula um processamento mais demorado
	return Results.Json(response);
});

/// <summary>
/// Limpa os dados de pagamentos
/// </summary>
app.MapPost("/admin/purge-payments", static async (ApiPaymentRepository paymentRepository) =>
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
