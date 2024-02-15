
namespace RinhaBackendCrebito.Codigo.Extrato
{
    public sealed class ExtratoResponse(Saldo saldo, Transacao[]? ultimas_transacoes)
    {
        public Saldo saldo { get; } = saldo;
        public Transacao[]? ultimas_transacoes { get; } = ultimas_transacoes;
    }

    public readonly struct Saldo(int _total, DateTime _data_extrato, int _limite)
    {
        public int total { get; } = _total;
        public DateTime data_extrato { get; } = _data_extrato;
        public int limite { get; } = _limite;
    }

    public struct Transacao()
    {
        public int valor { get; set; }
        public string tipo { get; set; }
        public string descricao { get; set; }
        public DateTime realizada_em { get; set; }
    }

    public struct ExtratoRaw()
    {
        public int total { get; set; }
        public DateTime data_extrato { get; set; }
        public int limite { get; set; }
        public string ultimas_transacoes { get; set; }
    }
}
