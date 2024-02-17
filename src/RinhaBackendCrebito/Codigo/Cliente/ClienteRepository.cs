using Npgsql;
using NpgsqlTypes;
using RinhaBackendCrebito.Codigo.Extrato;
using RinhaBackendCrebito.Codigo.Transacao;

namespace RinhaBackendCrebito.Codigo.Cliente
{
    public interface IClienteRepository
    {
        Task<IResult> ProcessarExtratoAsync(int id);
        Task<IResult> ProcessarTransacaoAsync(int id, TransacaoRequest transacaoRequest);
    }

    public class ClienteRepository(SqlConnectionFactory sqlConnectionFactory) : IClienteRepository
    {
        public async Task<IResult> ProcessarExtratoAsync(int id)
        {
            using (var connection = sqlConnectionFactory.Create())
            {
                var saldo = new Saldo();

                await connection.OpenAsync();
                await using var saldoCommand = new NpgsqlCommand("SELECT saldo as total, now() as data_extrato, limite FROM clientes WHERE id = @clienteid", connection);
                saldoCommand.Parameters.Add("@clienteid", NpgsqlDbType.Integer);
                //await saldoCommand.PrepareAsync();
                saldoCommand.Parameters[0].Value = id;
                
                await using var saldoDataReader = await saldoCommand.ExecuteReaderAsync();
                while (await saldoDataReader.ReadAsync())
                {
                    saldo.total = saldoDataReader.GetInt32(0);
                    saldo.data_extrato = saldoDataReader.GetDateTime(1);
                    saldo.limite = saldoDataReader.GetInt32(2);
                }
                await saldoDataReader.CloseAsync();

                var transacoes = new List<Extrato.Transacao>();
                await using var transacoesCommand = new NpgsqlCommand("SELECT valor, tipo, descricao, realizada_em FROM transacoes WHERE cliente_id = @clienteid ORDER BY realizada_em DESC LIMIT 10", connection);
                transacoesCommand.Parameters.Add("@clienteid", NpgsqlDbType.Integer);
                //await transacoesCommand.PrepareAsync();
                transacoesCommand.Parameters[0].Value = id;
                await using var transacoesDataReader = await transacoesCommand.ExecuteReaderAsync();

                while (await transacoesDataReader.ReadAsync())
                {
                    transacoes.Add(new Extrato.Transacao
                    {
                        valor = transacoesDataReader.GetInt32(0),
                        tipo = transacoesDataReader.GetString(1),
                        descricao = transacoesDataReader.GetString(2),
                        realizada_em = transacoesDataReader.GetDateTime(3)
                    });
                }
                await transacoesDataReader.CloseAsync();
                await connection.CloseAsync();

                return Results.Ok(new ExtratoResponse(saldo, transacoes.ToArray()));
            }
        }

        public async Task<IResult> ProcessarTransacaoAsync(int id, TransacaoRequest transacaoRequest)
        {
            var transacaoResponse = new TransacaoResponse();
            using (var connection = sqlConnectionFactory.Create())
            {
                await connection.OpenAsync();
                await using var TransacaoCommand = new NpgsqlCommand($"select {ObterNomeFuncaoTransacao(transacaoRequest.tipo)}(@clienteid_in, @valor_in, @descricao_in) as saldo", connection);
                TransacaoCommand.Parameters.Add("@clienteid_in", NpgsqlDbType.Integer);
                TransacaoCommand.Parameters.Add("@valor_in", NpgsqlDbType.Integer);
                TransacaoCommand.Parameters.Add("@descricao_in", NpgsqlDbType.Varchar);
                //await TransacaoCommand.PrepareAsync();
                TransacaoCommand.Parameters[0].Value = id;
                TransacaoCommand.Parameters[1].Value = transacaoRequest.valor;
                TransacaoCommand.Parameters[2].Value = transacaoRequest.descricao;

                await using var transacaoDataReader = await TransacaoCommand.ExecuteReaderAsync();
                while (await transacaoDataReader.ReadAsync())
                {
                    if(await transacaoDataReader.IsDBNullAsync(0))
                        return Results.UnprocessableEntity("Transação inválida");
                    
                    transacaoResponse.saldo = transacaoDataReader.GetInt32(0);
                }
                await transacaoDataReader.CloseAsync();

                transacaoResponse.limite = GetLimiteCliente(id);
                return Results.Ok(transacaoResponse);
            }
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
