using Azure;
using Azure.Data.Tables;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CLVD6212_POE.Models
{
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } = "Order";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }

        [NotMapped]
        public ETag ETag { get; set; }

        [Display(Name = "Order ID")]
        [JsonPropertyName("id")]  // ← camelCase
        public string OrderId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Customer")]
        [JsonPropertyName("customerId")]  // ← camelCase
        public string CustomerId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product ID")]
        [JsonPropertyName("productId")]  // ← camelCase
        public string ProductId { get; set; } = string.Empty;

        [Display(Name = "Product Name")]
        [JsonPropertyName("productName")]  // ← camelCase
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        [JsonPropertyName("quantity")]  // ← camelCase
        public int Quantity { get; set; }

        [Display(Name = "Unit Price")]
        [DataType(DataType.Currency)]
        [JsonPropertyName("unitPrice")]  // ← camelCase
        public double UnitPrice { get; set; }

        [Display(Name = "Total Price")]
        [DataType(DataType.Currency)]
        [JsonPropertyName("totalAmount")]  // ← camelCase (matches DTO)
        public double TotalPrice { get; set; }

        [Required]
        [Display(Name = "Order Date")]
        [DataType(DataType.DateTime)]
        [JsonPropertyName("orderDateUtc")]  // ← camelCase
        public DateTimeOffset OrderDate { get; set; }

        [Required]
        [Display(Name = "Order Status")]
        [JsonPropertyName("status")]  // ← camelCase
        public OrderStatus Status { get; set; } = OrderStatus.Submitted;

        public enum OrderStatus
        {
            Submitted,
            Processing,
            Completed,
            Cancelled
        }
    }
}