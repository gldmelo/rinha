
namespace RinhaBackend2025.Codigo.Worker
{
	public class WorkerPaymentBackgroundService(PaymentService paymentService) : BackgroundService
	{
		/// <summary>
		/// Encaminha a execução do serviço de pagamento para o PaymentService.
		/// </summary>
		protected override async Task ExecuteAsync(CancellationToken stoppingToken) 
			=> await paymentService.ExecuteAsync(stoppingToken);
	}
}
