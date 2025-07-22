using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using RinhaBackend2025.Codigo;
using StackExchange.Redis;

const string redisFila = "rinha";

#region DI
var builder = WebApplication.CreateSlimBuilder(args);
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
builder.Services.AddSingleton<IDatabase>(cfg =>
{
	var redisServer = Environment.GetEnvironmentVariable("RedisServer") ?? "localhost:6379";
	return ConnectionMultiplexer.Connect(redisServer).GetDatabase();
});

var app = builder.Build();
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
	await redis.ListLeftPushAsync(redisFila, rawBody);
	return Results.Ok();
});

/// <summary>
/// Retorna o resumo dos pagamentos processados.
/// </summary>
app.MapGet("/payments-summary", static async ([FromServices]PaymentService paymentService, DateTime? from, DateTime? to) =>
{
	var response = await paymentService.ObterResumoPagamentosAsync(from, to);
	return Results.Json(response);
});

/// <summary>
/// Limpa os dados de pagamentos
/// </summary>
app.MapPost("/admin/purge-payments", static async (
	ApiPaymentRepository paymentRepository) =>
{
	await paymentRepository.LimpezaDados();
	return Results.Ok();
});

await app.RunAsync();

#endregion

[JsonSerializable(typeof(PaymentSummaryResponse))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
