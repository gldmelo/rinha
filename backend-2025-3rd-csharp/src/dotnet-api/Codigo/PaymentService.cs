using System.Net;
using System.Text.Json;
using RinhaBackend2025.Codigo.Worker;
using StackExchange.Redis;

namespace RinhaBackend2025.Codigo
{
	/// <summary>
	/// Implementa as regras de negócio para processar pagamentos.
	/// </summary>
	public class PaymentService(
		IDatabase redis,
		PaymentProcessorClient defaultPaymentProcessor,
		PaymentProcessorClient fallbackPaymentProcessor,
		PaymentRepository paymentRepository)
	{
		private const string KeyFilaRedis = "rinha";

		public async Task<PaymentSummaryResponse> ObterResumoPagamentosAsync(DateTime? from, DateTime? to)
		{
			return await paymentRepository.ObterResumoPagamentos(from, to).ConfigureAwait(false);
		}

		public async Task<bool> TryAddPagamentoAsync(WorkerPaymentRequest paymentRequest)
		{
			if (paymentRequest == null)
				return false;

			await redis.ListRightPushAsync(KeyFilaRedis, JsonSerializer.Serialize(paymentRequest, AppJsonSerializerContext.Default.WorkerPaymentRequest)).ConfigureAwait(false);
			return true;
		}

		/// <summary>
		/// Executa o serviço de processamento de pagamentos dentro do Semáforo
		/// </summary>
		public async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				var redisEntry = await redis.ListLeftPopAsync(KeyFilaRedis).ConfigureAwait(false);

				if (!redisEntry.HasValue || redisEntry.IsNull)
					continue;

				var paymentRequest = JsonSerializer.Deserialize(redisEntry.ToString(), AppJsonSerializerContext.Default.WorkerPaymentRequest) ?? throw new ApplicationException("Não foi possível desserializar o pagamento");
				await ProcessarPagamento(paymentRequest).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Implementa a regra de negócio para processar pagamentos visando o maior lucro entre dois processadores de pagamento
		/// </summary>
		public async Task ProcessarPagamento(WorkerPaymentRequest paymentRequest)
		{
			char paymentProcessor = 'd';
			var statusCode = HttpStatusCode.NotFound;
			DateTime paymentTime = DateTime.UtcNow;
			do
			{
				try
				{
					paymentProcessor = 'd';
					statusCode = await defaultPaymentProcessor.LancarPagamentoPaymentProcessor(paymentRequest, paymentTime).ConfigureAwait(false);

					if (statusCode == HttpStatusCode.UnprocessableContent)
					{
						paymentRequest.CorrelationId = Guid.CreateVersion7();
						continue;
					}

					if (statusCode != HttpStatusCode.OK)
					{
						paymentProcessor = 'f';
						statusCode = await fallbackPaymentProcessor.LancarPagamentoPaymentProcessor(paymentRequest, paymentTime).ConfigureAwait(false);
						if (statusCode == HttpStatusCode.UnprocessableContent)
						{
							paymentRequest.CorrelationId = Guid.CreateVersion7();
							continue;
						}
					}
				}
				catch (Exception)
				{
					
				}
			} while (statusCode != HttpStatusCode.OK);

			await paymentRepository.SalvarPagamento(paymentRequest.AsPaymentRequestStore(paymentTime, paymentProcessor)).ConfigureAwait(false);
		}

	}
}
