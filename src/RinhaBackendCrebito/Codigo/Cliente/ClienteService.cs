using System.Data;
using Dapper;
using RinhaBackendCrebito.Codigo.Extrato;
using RinhaBackendCrebito.Codigo.Transacao;

namespace RinhaBackendCrebito.Codigo.Cliente
{
    public sealed class ClienteService(SqlConnectionFactory connectionFactory)
    {
        public async Task<IResult> ProcessarExtratoAsync(int id)
        {
            if (IsClienteInvalido(id))
                return Results.NotFound("Cliente não encontrado.");

            using (var connection = connectionFactory.Create())
            {
                await connection.OpenAsync();
                var saldo = await connection.QuerySingleAsync<Saldo>(@"SELECT saldo as total, now() as data_extrato, limite FROM clientes WHERE id = @clienteid_in", 
                    commandType: CommandType.Text, 
                    param: new { clienteid_in = id });

                var transacoes = await connection.QueryAsync<Extrato.Transacao>(@"SELECT valor, tipo, descricao, realizada_em FROM transacoes WHERE cliente_id = @clienteid_in ORDER BY realizada_em DESC LIMIT 10", 
                    commandType: CommandType.Text, 
                    param: new { clienteid_in = id });

                await connection.CloseAsync();
                
                return Results.Ok(new ExtratoResponse(saldo, transacoes.ToArray()));
            }
        }

        public async Task<IResult> ProcessarTransacaoAsync(int id, TransacaoRequest transacaoRequest)
        {
            if (IsClienteInvalido(id))
                return Results.NotFound("Cliente não encontrado.");

            if (IsTransacaoInvalida(transacaoRequest))
                return Results.UnprocessableEntity("Transação inválida");

            using (var connection = connectionFactory.Create())
            {
                await connection.OpenAsync();
                var transacaoResponse = await connection.QuerySingleOrDefaultAsync<TransacaoResponse>(
                    $"select {ObterNomeFuncaoTransacao(transacaoRequest.tipo)}(@clienteid_in, @valor_in, @descricao_in) as saldo",
                    commandType: CommandType.Text,
                    param: new
                    {
                        clienteid_in = id,
                        valor_in = transacaoRequest.valor,
                        descricao_in = transacaoRequest.descricao
                    });
                await connection.CloseAsync();

                if (transacaoResponse == null)
                    return Results.UnprocessableEntity("Transação inválida");

                transacaoResponse.limite = GetLimiteCliente(id);
                return Results.Ok(transacaoResponse);
            }
        }

        public static bool IsClienteInvalido(int id)
        {
            return id <= 0 || id > 5;
        }

        public static bool IsTransacaoInvalida(TransacaoRequest transacao)
        {
            return transacao.valor <= 0
                || string.IsNullOrEmpty(transacao.descricao) || transacao.descricao.Length > 10
                || transacao.tipo != "c" && transacao.tipo != "d";
        }

        public static string ObterNomeFuncaoTransacao(string tipo)
        {
            return tipo switch
            {
                "d" => "inserir_transacao_debito_e_retornar_saldo",
                _ => "inserir_transacao_credito_e_retornar_saldo"
            };
        }

        public static int GetLimiteCliente(int id)
        {
            var limites = new int[] { 100000, 80000, 1000000, 10000000, 500000 };
            return limites[id - 1];
        }
        
    }
}
