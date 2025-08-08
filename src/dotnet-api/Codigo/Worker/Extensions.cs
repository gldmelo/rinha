
namespace RinhaBackend2025.Codigo.Worker
{
	public static class Extensions
	{
		public static PaymentRequestStore AsPaymentRequestStore(this WorkerPaymentRequest paymentRequest, char paymentProcessor)
			=> new(paymentRequest.CorrelationId, paymentRequest.Amount, DateTime.UtcNow, paymentProcessor);
	}
}
