using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Newtonsoft.Json;
using RinhaBackendCrebito.Codigo;
using RinhaBackendCrebito.Codigo.Extrato;
using RinhaBackendCrebito.Codigo.Transacao;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddSingleton(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConfiguration") ?? throw new ApplicationException("Connection String não foi informada");
    
    return new SqlConnectionFactory(connectionString);
});

var slidingPolicy = "concurrency";

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = 429;
    o.AddConcurrencyLimiter(policyName: slidingPolicy, options =>
    {
        options.PermitLimit = 30;
        options.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 30000;

    });
});

var app = builder.Build();

app.UseRateLimiter();

app.MapGet("/clientes/{id}/extrato", async ([FromServices] SqlConnectionFactory connectionFactory, int id) =>
    {
        if (IsClienteInValido(id))
            return Results.NotFound("Cliente não encontrado.");

        Saldo saldo;
        Transacao [] transacoes;

        var sql = @"SELECT ""saldo"" as total, now() as data_extrato, c.limite,
                    (
                        SELECT json_agg(json_build_object(
                            'valor', t.valor,
                            'tipo', t.tipo,
                            'descricao', t.descricao,
                            'realizada_em', t.realizada_em
                        ))
                        FROM (
                            SELECT valor, tipo, descricao, realizada_em
                            FROM transacoes
                            WHERE cliente_id = c.id
                            ORDER BY realizada_em DESC
                            LIMIT 10
                        ) AS t
                    ) AS ultimas_transacoes
                    FROM clientes c WHERE c.id = @clienteid_in;";

        using var connection = connectionFactory.Create();
        await connection.OpenAsync();
        var extratoRaw = await connection.QuerySingleAsync<ExtratoRaw>(sql, commandType: CommandType.Text, param: new { clienteid_in = id });
        saldo = new Saldo(extratoRaw.total, extratoRaw.data_extrato, extratoRaw.limite);
        if (extratoRaw.ultimas_transacoes != null)
            transacoes = JsonConvert.DeserializeObject<Transacao[]>(extratoRaw.ultimas_transacoes);
        else
            transacoes = [];

        await connection.CloseAsync();
        return Results.Ok(new ExtratoResponse(saldo, transacoes));
    })
    .RequireRateLimiting(slidingPolicy)
    .DisableRequestTimeout();

app.MapPost("/clientes/{id}/transacoes", async (int id, [FromServices] SqlConnectionFactory connectionFactory, HttpContext context
    /*[FromBody] TransacaoRequest transacao */) =>
    {
        TransacaoRequest transacao;
        try
        {
            transacao = await context.Request.ReadFromJsonAsync<TransacaoRequest>();
        }
        catch (Exception)
        {
            return Results.UnprocessableEntity("Transação inválida");
        }

        if (IsClienteInValido(id))
            return Results.NotFound("Cliente não encontrado.");

        if (IsTransacaoInvalida(transacao!))
            return Results.UnprocessableEntity("Transação inválida");

        var strFuncaoTransaction = "inserir_transacao_credito_e_retornar_saldo";

        if (transacao!.tipo.Equals("d"))
            strFuncaoTransaction = "inserir_transacao_debito_e_retornar_saldo";

        using var connection = connectionFactory.Create();
        await connection.OpenAsync();
        var transacaoResponse = await connection.QuerySingleOrDefaultAsync<TransacaoResponse>($"select {strFuncaoTransaction}(@clienteid_in, @valor_in, @descricao_in) as saldo",
            commandType: CommandType.Text,
            param: new
            {
                clienteid_in = id,
                valor_in = transacao.valor,
                descricao_in = transacao.descricao
            });
        
        if (transacaoResponse == null)
            return Results.UnprocessableEntity("Transação inválida");

        transacaoResponse.limite = GetLimiteCliente(id);
        await connection.CloseAsync();

        return Results.Ok(transacaoResponse);
    })
    .RequireRateLimiting(slidingPolicy)
    .DisableRequestTimeout();

app.Run();

static bool IsClienteInValido(int id)
{
    return id <= 0 || id > 5;
}

static bool IsTransacaoInvalida(TransacaoRequest transacao)
{
    if (transacao.valor <= 0 
        || string.IsNullOrEmpty(transacao.descricao) || transacao.descricao.Length > 10 
        || (transacao.tipo != "c" && transacao.tipo != "d"))
        return true;

    return false;
}

static int GetLimiteCliente(int id)
{
    var limites = new int[] { 100000, 80000, 1000000, 10000000, 500000 };
    return limites[id - 1];
}