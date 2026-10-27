
using Newtonsoft.Json;

namespace Mango.Services.OrderAPI.Messages
{
    public class OrderCreatedEvent
    {
        [JsonProperty("eventId")]
        public Guid EventId { get; set; } = Guid.NewGuid();

        [JsonProperty("eventType")]
        public string EventType { get; set; } = "OrderCreated";

        [JsonProperty("contractVersion")]
        public int ContractVersion { get; set; } = 1;

        [JsonProperty("occurredAtUtc")]
        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

        [JsonProperty("orderId")]
        public int OrderId { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonProperty("customerEmail")]
        public string CustomerEmail { get; set; } = string.Empty;

        [JsonProperty("totalAmount")]
        public double TotalAmount { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; } = "INR";

        [JsonProperty("items")]
        public List<OrderCreatedItem> Items { get; set; } = new();
    }

    public class OrderCreatedItem
    {
        [JsonProperty("productId")]
        public int ProductId { get; set; }

        [JsonProperty("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("unitPrice")]
        public double UnitPrice { get; set; }
    }
}
