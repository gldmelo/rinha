
namespace RinhaBackendCrebito.Codigo.Extrato
{
    public sealed class ExtratoResponse(Saldo saldo, Transacao[]? ultimas_transacoes)
    {
        public Saldo saldo { get; } = saldo;
        public Transacao[]? ultimas_transacoes { get; } = ultimas_transacoes;
    }

    public class Saldo
    {
        public int total { get; set; }
        public DateTime data_extrato { get; set; }
        public int limite { get; set; }
    }

    public class Transacao()
    {
        public int valor { get; set; }
        public string tipo { get; set; }
        public string descricao { get; set; }
        public DateTime realizada_em { get; set; }
    }

}
