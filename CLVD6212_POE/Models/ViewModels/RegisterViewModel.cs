using System.ComponentModel.DataAnnotations;

namespace CLVD6212_POE.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage ="Username is required")]
        public string Username { get; set; }= string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage ="Invalid Email Address")]
        public string Email { get; set; }= string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }= string.Empty;

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }= string.Empty;

        [Required(ErrorMessage = "Surname is required")]
        public string Surname { get; set; }= string.Empty;

        [Required(ErrorMessage = "Shipping Address is required")]
        [Display(Name ="Shipping Address")]
        public string ShippingAddress { get; set; } = string.Empty;

        public string Role { get; set; } = "Customer";//default role

        //public enum Roles
        //{
        //    Customer,
        //    Admin
        //}
    }
}
