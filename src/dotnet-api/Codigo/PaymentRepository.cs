using Npgsql;
using RinhaBackend2025.Codigo.Worker;

namespace RinhaBackend2025.Codigo
{
	public class PaymentRepository(SqlConnectionFactory sqlConnectionFactory)
	{
		public async Task<PaymentSummaryResponse> ObterResumoPagamentos(DateTime? from, DateTime? to)
		{
			using var conn = sqlConnectionFactory.Create();
			await conn.OpenAsync();
			await using var comm = new NpgsqlCommand("SELECT payment_processor, COUNT(*) AS total_requests, COALESCE(SUM(amount),0) AS total_amount FROM payments WHERE requested_at BETWEEN @from AND @to GROUP BY payment_processor", conn);
			comm.Parameters.AddWithValue("from", from ?? DateTime.MinValue);
			comm.Parameters.AddWithValue("to", to ?? DateTime.MaxValue);
			var response = new PaymentSummaryResponse();
			await using (var reader = await comm.ExecuteReaderAsync())
			{
				while (await reader.ReadAsync())
				{
					var processor = reader.GetString(0);
					var totalRequests = reader.GetInt32(1);
					var totalAmount = reader.GetDecimal(2);
					if (processor == "d")
						response.Default = new PaymentProcessorSummary { TotalRequests = totalRequests, TotalAmount = totalAmount };
					else
						response.Fallback = new PaymentProcessorSummary { TotalRequests = totalRequests, TotalAmount = totalAmount };
				}
			}
			return response;
		}

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

		public async Task<bool> ContemRegistro(Guid correlationId)
		{
			using var conn = sqlConnectionFactory.Create();
			await conn.OpenAsync();
			using var comm = new NpgsqlCommand("SELECT COUNT(*) FROM payments where correlationId = @correlationId", conn);
			comm.Parameters.AddWithValue("correlationId", correlationId);
			var count = (long)await comm.ExecuteScalarAsync();

			return count > 0;
		}

		public async Task LimpezaDados()
		{
			using var conn = sqlConnectionFactory.Create();
			await conn.OpenAsync();
			await using var transacaoCommand = new NpgsqlCommand("TRUNCATE TABLE payments RESTART IDENTITY", conn);
			await transacaoCommand.ExecuteNonQueryAsync();
		}
	}
}
