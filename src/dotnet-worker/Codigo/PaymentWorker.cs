using System.Text.Json;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace BackgroundWorker.Codigo
{
	public class PaymentWorker(IDatabase redis, PaymentService paymentService) : BackgroundService
	{
		private const string QueueName = "rinha";

		/// <summary>
		/// Pega um pagamento da fila do Redis e processa.
		/// </summary>
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				var redisEntry = await redis.ListRightPopAsync(QueueName);

				if (!redisEntry.HasValue || redisEntry.IsNull)
				{
					await Task.Delay(1000, stoppingToken);
					continue;
				}

				var paymentRequest = JsonSerializer.Deserialize<PaymentRequest>(redisEntry.ToString(), AppJsonSerializerContext.Default.PaymentRequest) ?? throw new ApplicationException("Não foi possível desserializar o pagamento");
				await paymentService.ProcessarPagamento(paymentRequest);
			}
		}
	}
}
