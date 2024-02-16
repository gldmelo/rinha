using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RinhaBackendCrebito.Codigo;
using RinhaBackendCrebito.Codigo.Cliente;
using RinhaBackendCrebito.Codigo.Transacao;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddSingleton(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    //var connectionString = configuration.GetConnectionString("DefaultConfiguration") ?? throw new ApplicationException("Connection String não foi informada");
    var connectionString = configuration.GetConnectionString("PROD") ?? throw new ApplicationException("Connection String não foi informada");

    return new SqlConnectionFactory(connectionString);
});

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

var app = builder.Build();

app.UseRateLimiter();

app.MapGet("/clientes/{id}/extrato", async ([FromServices] SqlConnectionFactory connectionFactory, int id) =>
    {
        return await new ClienteService(connectionFactory).ProcessarExtratoAsync(id);
    })
    .RequireRateLimiting(slidingPolicy)
    .DisableRequestTimeout();

app.MapPost("/clientes/{id}/transacoes", async (int id, [FromServices] SqlConnectionFactory connectionFactory, HttpContext context
    /*[FromBody] TransacaoRequest transacao)*/ ) => 
    {
        TransacaoRequest? transacao;
        try
        {
            transacao = await context.Request.ReadFromJsonAsync<TransacaoRequest>();
        }
        catch (Exception)
        {
            return Results.UnprocessableEntity("Transação inválida");
        }

        return await new ClienteService(connectionFactory).ProcessarTransacaoAsync(id, transacao!);
    }).RequireRateLimiting(slidingPolicy)
      .DisableRequestTimeout();

app.Run();

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(RinhaBackendCrebito.Codigo.Extrato.Transacao[]))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
