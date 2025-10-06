using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CLVD6212_POE.Models
{
    public class Customer : ITableEntity
    {
        public string PartitionKey { get; set; } = "Customer";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Display(Name = "Customer ID")]
        [JsonPropertyName("id")]  // ← MUST be lowercase "id"
        public string CustomerId { get; set; } = string.Empty;  // ← Regular property with default

        [Required]
        [Display(Name = "First Name")]
        [JsonPropertyName("name")]  // ← lowercase "name"
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Surname")]
        [JsonPropertyName("surname")]  // ← lowercase "surname"
        public string Surname { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Username")]
        [JsonPropertyName("username")]  // ← lowercase "username"
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        [JsonPropertyName("email")]  // ← lowercase "email"
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Shipping Address")]
        [JsonPropertyName("shippingAddress")]  // ← lowercase "shippingAddress"
        public string ShippingAddress { get; set; } = string.Empty;
    }
}