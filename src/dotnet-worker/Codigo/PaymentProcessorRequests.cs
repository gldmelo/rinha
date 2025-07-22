using System.Text.Json.Serialization;

namespace BackgroundWorker.Codigo
{
	public class PaymentRequest
	{
		[JsonPropertyName("correlationId")]
		public Guid CorrelationId { get; set; } = Guid.Empty;

		[JsonPropertyName("amount")]
		public decimal Amount { get; set; } = 0m;
	}

	public class PaymentProcessorRequest
	{
		[JsonPropertyName("correlationId")]
		public Guid CorrelationId { get; set; } = Guid.Empty;

		[JsonPropertyName("amount")]
		public decimal Amount { get; set; } = 0m;
		
		[JsonPropertyName("requestedAt")]
		public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
	}

	public record PaymentRequestStore(Guid CorrelationId, decimal Amount, DateTime RequestedAt, char PaymentProcessor);

	public record HealthCheckResponse(bool Failing, int MinResponseTime); 

}
