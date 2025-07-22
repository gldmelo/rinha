
namespace BackgroundWorker.Codigo
{
	public static class Extensions
	{
		public static PaymentRequestStore AsPaymentRequestStore(this PaymentRequest paymentRequest, char paymentProcessor)
			=> new(paymentRequest.CorrelationId, paymentRequest.Amount, DateTime.UtcNow, paymentProcessor);
	}
}
