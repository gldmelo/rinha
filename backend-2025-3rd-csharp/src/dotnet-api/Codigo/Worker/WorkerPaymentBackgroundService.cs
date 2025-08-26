
namespace RinhaBackend2025.Codigo.Worker
{
	public class WorkerPaymentBackgroundService(PaymentService paymentService) : BackgroundService
	{
		/// <summary>
		/// Encaminha a execu��o do servi�o de pagamento para o PaymentService.
		/// </summary>
		protected override async Task ExecuteAsync(CancellationToken stoppingToken) 
			=> await paymentService.ExecuteAsync(stoppingToken);
	}
}
