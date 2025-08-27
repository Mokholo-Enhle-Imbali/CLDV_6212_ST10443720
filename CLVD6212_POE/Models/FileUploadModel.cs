using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace CLVD6212_POE.Models
{
    public class FileUploadModel
    {
        [Required]
        [Display(Name ="Proof of Payment")]
        public IFormFile? ProofOfPayment { get; set; }

        [Display(Name ="Order ID")]
        public string? OrderId { get; set; }

        [Display(Name = "Customer Name")]
        public string? CustomerName { get; set; }
       

      
    }
}
