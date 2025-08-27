using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace CLVD6212_POE.Models
{
    public class Product: ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; }=Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Display(Name ="Product ID")]
        public string ProductId => RowKey;

        [Required(ErrorMessage ="Product Name is required")]
        [Display(Name ="Product Name")]
        public string ProductName { get; set; }=string.Empty;

        [Required(ErrorMessage ="Description is required")]
        [Display(Name = "Description")]
        public string Description { get; set; }=string.Empty;

        [Required(ErrorMessage ="Price is Required")]
        [Range(0.01,double.MaxValue,ErrorMessage ="Price must be greater than 0")]
        [Display( Name = "Price")]
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
        public int StockAvailable { get; set; }


        [Display(Name ="Image URL")]
        public string ImageUrl { get; set; }=string.Empty;
      
       
    }
}
