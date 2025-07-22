
namespace RinhaBackend2025.Codigo
{
	public class PaymentService(ApiPaymentRepository paymentRepository)
	{
		public async Task<PaymentSummaryResponse> ObterResumoPagamentosAsync(DateTime? from, DateTime? to)
		{
			return await paymentRepository.ObterResumoPagamentos(from, to);
		}
	}
}
