using Azure;
using Azure.Data.Tables;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CLVD6212_POE.Models
{
    public class Order: ITableEntity
    {

        public string PartitionKey { get; set; } = "Order";

        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        //public Order()
        //{
        //    // Only generate RowKey if it's not already set
        //    if (string.IsNullOrEmpty(RowKey))
        //    {
        //        RowKey = Guid.NewGuid().ToString();
        //    }
        //    PartitionKey = "Order";
        //}

       
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Display(Name="Order ID")]
        public string OrderId => RowKey;
        [Required]
        [Display(Name ="Product ID")]
         public string ProductId { get; set; }= string.Empty;
        [Required]
        [Display(Name ="Customer")]
        public string CustomerId { get; set; }=string.Empty;

       
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Display(Name ="Order Date")]
        [DataType(DataType.DateTime)]
        public DateTime OrderDate { get; set; }

        [Required]
        [Display(Name = "Quantity")]
        [Range(1,int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantitiy { get; set; }

        
        [Display(Name = "Unit Price")]
        [DataType(DataType.Currency)]
        public double UnitPrice { get; set; }


        [Display(Name = "Total Price")]
        [DataType(DataType.Currency)]
        public double TotalPrice { get; set; }

        [Required]
        [Display(Name = "Order Status")]
        public string Status { get; set; } = "Submitted";

   
        public enum OrderStatus
        {
            Submitted,
            Processing,
            Completed,
            Canceled
        }
       
    }
}
