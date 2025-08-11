using System.Net;
using System.Text.Json;

namespace RinhaBackend2025.Codigo.Worker
{
	/// <summary>
	/// Executa as operações de pagamento contra o PaymentProcessor.
	/// </summary>
	public class PaymentProcessorClient(string baseAdress, decimal fee)
	{
		public DateTime UltimaVerificacaoHealthCheck { get; private set; } = DateTime.MinValue;
		public HealthCheckResponse UltimoHealthCheck { get; private set; } = new HealthCheckResponse(false, 0);
		public decimal Fee { get; } = fee;

		private readonly HttpClient httpClient = new()
		{
			BaseAddress = new Uri(baseAdress), 
			Timeout = TimeSpan.FromMilliseconds(200)
		};

		public async Task<HttpStatusCode> LancarPagamentoPaymentProcessor(WorkerPaymentRequest paymentsRequest, DateTime paymentTime)
		{
			var json = new PaymentProcessorRequest
			{
				CorrelationId = paymentsRequest.CorrelationId,
				Amount = paymentsRequest.Amount,
				RequestedAt = paymentTime
			};

			var jsonContent = new StringContent(
				JsonSerializer.Serialize(json, AppJsonSerializerContext.Default.PaymentProcessorRequest), 
				System.Text.Encoding.UTF8, 
				"application/json");

			var response = await httpClient.PostAsync("payments", jsonContent);

			return response.StatusCode;
		}
		
		/// <summary>
		/// Verifica a saúde do PaymentProcessor.
		/// </summary>
		public async Task HealthCheck()
		{
			// Se já verificou recentemente, retorna um valor padrão
			if (UltimaVerificacaoHealthCheck.AddSeconds(5) > DateTime.Now)
				return;
			
			var response = await httpClient.GetAsync("/payments/service-health");
			UltimoHealthCheck = await response.Content.ReadFromJsonAsync(AppJsonSerializerContext.Default.HealthCheckResponse);
			UltimaVerificacaoHealthCheck = DateTime.Now;
		}
	}
}
