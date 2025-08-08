using System.Text.Json;
using StackExchange.Redis;

namespace RinhaBackend2025.Codigo.Worker
{
	public class WorkerPaymentBackgroundService(IDatabase redis, WorkerPaymentService paymentService) : BackgroundService
	{
		private const string QueueName = "rinha";

		/// <summary>
		/// Pega um pagamento da fila do Redis e processa.
		/// </summary>
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				var redisEntry = await redis.ListLeftPopAsync(QueueName);

				if (!redisEntry.HasValue || redisEntry.IsNull)
				{
					//await Task.Delay(50, stoppingToken);
					continue;
				}

				var paymentRequest = JsonSerializer.Deserialize(redisEntry.ToString(), AppJsonSerializerContext.Default.WorkerPaymentRequest) ?? throw new ApplicationException("Não foi possível desserializar o pagamento");
				await paymentService.ProcessarPagamento(paymentRequest);
			}
		}
	}
}
