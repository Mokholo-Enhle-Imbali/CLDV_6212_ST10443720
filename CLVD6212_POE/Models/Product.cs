using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CLVD6212_POE.Models
{
    public class Product: ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; }=Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Display(Name ="Product ID")]
        [JsonPropertyName("id")]
        public string? ProductId { get; set; } = string.Empty;

        [Required(ErrorMessage ="Product Name is required")]
        [Display(Name ="Product Name")]
        [JsonPropertyName("productName")]
        public string ProductName { get; set; }=string.Empty;

        [Required(ErrorMessage ="Description is required")]
        [Display(Name = "Description")]
        [JsonPropertyName("description")]
        public string Description { get; set; }=string.Empty;

        [Required(ErrorMessage ="Price is Required")]
        [Range(0.01,double.MaxValue,ErrorMessage ="Price must be greater than 0")]
        [Display( Name = "Price")]
        [JsonPropertyName("price")]
        public double Price { get; set; }

        public string priceString
        {
            get => Price.ToString("F2");
            set
            {
                if (double.TryParse(value, out var result))
                    Price = result;
                else
                    Price = 0.0;
            }
        }

        [Required(ErrorMessage ="The stock available is required")]
        [Display(Name ="Stock Available")]
        [JsonPropertyName("stockAvailable")]
        public int StockAvailable { get; set; }


        [Display(Name ="Image URL")]
        [JsonPropertyName("imageUrl")]
        public string ImageUrl { get; set; }=string.Empty;
      
       
    }
}
