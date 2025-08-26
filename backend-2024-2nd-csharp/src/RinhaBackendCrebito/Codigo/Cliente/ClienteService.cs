using RinhaBackendCrebito.Codigo.Transacao;

namespace RinhaBackendCrebito.Codigo.Cliente
{
    public interface IClienteService
    {
        Task<IResult> ProcessarExtratoAsync(int id);
        Task<IResult> ProcessarTransacaoAsync(int id, TransacaoRequest transacaoRequest);
    }

    public sealed class ClienteService(IClienteRepository clienteRepository) : IClienteService
    {
        public async Task<IResult> ProcessarExtratoAsync(int id)
        {
            if (IsClienteInvalido(id))
                return Results.NotFound();
            
            return await clienteRepository.ProcessarExtratoAsync(id);
        }

        public async Task<IResult> ProcessarTransacaoAsync(int id, TransacaoRequest transacaoRequest)
        {
            if (IsClienteInvalido(id))
                return Results.NotFound();

            if (IsTransacaoInvalida(transacaoRequest))
                return Results.UnprocessableEntity();
            
            return await clienteRepository.ProcessarTransacaoAsync(id, transacaoRequest);
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
    }
}
