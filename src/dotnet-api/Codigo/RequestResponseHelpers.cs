using System.Text.Json.Serialization;

namespace RinhaBackend2025.Codigo
{
	public record PaymentRequest(Guid CorrelationId, decimal Amount);

	public record PaymentSummaryRequest(DateTime From, DateTime To);

	public class PaymentSummaryResponse
	{
		[JsonPropertyName("default")]
		public PaymentProcessorSummary Default { get; set; } = new PaymentProcessorSummary();

		[JsonPropertyName("fallback")]
		public PaymentProcessorSummary Fallback { get; set; } = new PaymentProcessorSummary();
	}

	public class PaymentProcessorSummary
	{
		[JsonPropertyName("totalRequests")]
		public int TotalRequests { get; set; }

		[JsonPropertyName("totalAmount")]
		public decimal TotalAmount { get; set; }
	}
}
