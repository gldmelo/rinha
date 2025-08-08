using System.Net;
using Polly;
using Polly.Retry;

namespace RinhaBackend2025.Codigo.Worker
{
	/// <summary>
	/// Implementa as regras de negócio para processar pagamentos.
	/// </summary>
	public class WorkerPaymentService(PaymentProcessorClient defaultPaymentProcessor, PaymentProcessorClient fallbackPaymentProcessor, WorkerPaymentRepository paymentRepository)
	{
		private static AsyncRetryPolicy DefaultRetryPolicy
			=> Policy.Handle<Exception>().WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromMilliseconds(100));

		/// <summary>
		/// Implementa a regra de negócio para processar pagamentos visando o maior lucro entre dois processadores de pagamento
		/// </summary>
		public async Task ProcessarPagamento(WorkerPaymentRequest paymentRequest)
		{
			char paymentProcessor;
			var statusCode = HttpStatusCode.NotFound;

			do
			{
				//if (defaultPaymentProcessor.UltimaVerificacaoHealthCheck.AddSeconds(5) <= DateTime.Now)
				//	await defaultPaymentProcessor.HealthCheck();

				//paymentProcessor = 'd';
				//if (defaultPaymentProcessor.UltimoHealthCheck.Failing)
				//{
				//	if (defaultPaymentProcessor.UltimoHealthCheck.MinResponseTime <= 1000)
				//		Thread.Sleep(defaultPaymentProcessor.UltimoHealthCheck.MinResponseTime);
				//}
				//else
				//{
				//	if (!defaultPaymentProcessor.UltimoHealthCheck.Failing)
				//	{
				//		statusCode = await defaultPaymentProcessor.LancarPagamentoPaymentProcessor(paymentRequest); // Tenta inserir de forma otimista inicialmente.
				//		if (statusCode == HttpStatusCode.OK)
				//			break;
				//	}
				//}

				paymentProcessor = 'd';
				await DefaultRetryPolicy.ExecuteAsync(async () =>
				{
					statusCode = await defaultPaymentProcessor.LancarPagamentoPaymentProcessor(paymentRequest);
				});

				if (statusCode != HttpStatusCode.OK)
				{
					paymentProcessor = 'f';
					await DefaultRetryPolicy.ExecuteAsync(async () =>
					{
						statusCode = await fallbackPaymentProcessor.LancarPagamentoPaymentProcessor(paymentRequest);
					});
				}

			} while (statusCode != HttpStatusCode.OK);

			await paymentRepository.SalvarPagamento(paymentRequest.AsPaymentRequestStore(paymentProcessor));
		}
	}
}
