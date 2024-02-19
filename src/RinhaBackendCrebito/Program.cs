using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RinhaBackendCrebito.Codigo;
using RinhaBackendCrebito.Codigo.Cliente;
using RinhaBackendCrebito.Codigo.Extrato;
using RinhaBackendCrebito.Codigo.Transacao;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddSingleton(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    //var connectionString = configuration.GetConnectionString("DefaultConfiguration") ?? throw new ApplicationException("Connection String não foi informada");
    var connectionString = configuration.GetConnectionString("PROD") ?? throw new ApplicationException("Connection String não foi informada");

    return new SqlConnectionFactory(connectionString);
});

builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();

var slidingPolicy = "concurrency";
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddConcurrencyLimiter(policyName: slidingPolicy, options =>
    {
        options.PermitLimit = 20;
        options.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 30000;
    });
});
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, SourceGenerationContext.Default);
});

var app = builder.Build();

app.UseRateLimiter();

app.MapGet("/clientes/{id}/extrato", async ([FromServices] IClienteService clienteService, int id) =>
    {
        return await clienteService.ProcessarExtratoAsync(id);
    })
    .RequireRateLimiting(slidingPolicy)
    .DisableRequestTimeout();

app.MapPost("/clientes/{id}/transacoes", async (int id, [FromServices] IClienteService clienteService, HttpContext context) => 
    {
        TransacaoRequest? transacao;
        try
        {
            transacao = await context.Request.ReadFromJsonAsync<TransacaoRequest>();
        }
        catch (Exception ex)
        {
            return Results.UnprocessableEntity();
        }

        return await clienteService.ProcessarTransacaoAsync(id, transacao!);
    }).RequireRateLimiting(slidingPolicy)
      .DisableRequestTimeout();

app.Run();

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Saldo))]
[JsonSerializable(typeof(Transacao))]
[JsonSerializable(typeof(TransacaoRequest))] 
[JsonSerializable(typeof(TransacaoResponse))]
[JsonSerializable(typeof(ExtratoResponse))]
[JsonSerializable(typeof(Transacao[]))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
