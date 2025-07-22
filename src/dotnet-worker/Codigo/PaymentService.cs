using System.Net;

namespace BackgroundWorker.Codigo
{
	/// <summary>
	/// Implementa as regras de negócio para processar pagamentos.
	/// </summary>
	public class PaymentService(PaymentProcessorClient defaultPaymentProcessor, PaymentProcessorClient fallbackPaymentProcessor, PaymentRepository paymentRepository)
	{
		/// <summary>
		/// Implementa a regra de negócio para processar pagamentos visando o maior lucro entre dois processadores de pagamento
		/// </summary>
		public async Task ProcessarPagamento(PaymentRequest paymentRequest)
		{
			HttpStatusCode statusCode = HttpStatusCode.NotImplemented;
			var paymentProcessor = 'd';
			try
			{
				//Fluxo: tenta inserir de forma otimista inicialmente.
				statusCode = await defaultPaymentProcessor.LancarPagamentoPaymentProcessor(paymentRequest);
			}
			catch (Exception)
			{
				//Fluxo: se falhar, tenta inserir de forma pessimista.
				statusCode = await fallbackPaymentProcessor.LancarPagamentoPaymentProcessor(paymentRequest);
				paymentProcessor = 'f';
			}
			finally
			{
				if (statusCode == HttpStatusCode.OK)
					await paymentRepository.SalvarPagamento(paymentRequest.AsPaymentRequestStore(paymentProcessor));
			}
		}

	}
}
