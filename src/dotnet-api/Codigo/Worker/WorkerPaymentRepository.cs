using Npgsql;

namespace RinhaBackend2025.Codigo.Worker
{
	public class WorkerPaymentRepository(SqlConnectionFactory sqlConnectionFactory)
	{
		public async Task SalvarPagamento(PaymentRequestStore payment)
		{
			using var conn = sqlConnectionFactory.Create();
			await conn.OpenAsync();
			await using var comm = new NpgsqlCommand("INSERT INTO payments (correlationId, amount, requested_at, payment_processor) VALUES (@correlationId, @amount, @requested_at, @payment_processor)", conn);
			comm.Parameters.AddWithValue("correlationId", payment.CorrelationId);
			comm.Parameters.AddWithValue("amount", payment.Amount);
			comm.Parameters.AddWithValue("requested_at", payment.RequestedAt);
			comm.Parameters.AddWithValue("payment_processor", payment.PaymentProcessor);

			await comm.ExecuteNonQueryAsync();
		}

	}
}
