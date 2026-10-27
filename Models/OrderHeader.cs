using System.ComponentModel.DataAnnotations;

namespace Mango.Services.OrderAPI.Models
{
    public class OrderHeader
    {
        [Key]
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? CouponCode { get; set; }
        public double Discount { get; set; }
        public double OrderTotal { get; set; }

        //TODO: As per Tutor used here Name. Try to reduce props for to Name in other services as well.
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Status { get; set; }
        public DateTime OrderTime { get; set; }
        public string? StripeSessionId { get; set; }
        public string? PaymentIntentId { get; set; }
        public IEnumerable<OrderDetails> OrderDetails { get; set; }
    }
}
