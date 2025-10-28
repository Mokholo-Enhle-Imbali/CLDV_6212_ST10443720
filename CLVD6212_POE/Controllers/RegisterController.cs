using CLVD6212_POE.Data;
using CLVD6212_POE.Models;
using CLVD6212_POE.Models.ViewModels;
using CLVD6212_POE.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLVD6212_POE.Controllers
{
    public class RegisterController : Controller
    {
        private readonly abcretailersDbContext _db;
        private readonly IFunctionsApi _functionsApi;

        public RegisterController(abcretailersDbContext db, IFunctionsApi functionsApi)
        {
            _db = db;
            _functionsApi = functionsApi;
        }

        // GET: /Register
        public IActionResult Index()
        {
            return View();
        }

        // POST: /Register
        [HttpPost]
        public async Task<IActionResult> Index(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check if username already exists
            var exists = await _db.Users.AnyAsync(u => u.Username == model.Username);
            if (exists)
            {
                ViewBag.Error = "Username already exists.";
                return View(model);
            }

            // ✅ 1. Save to SQL: create User
            var user = new User
            {
                Username = model.Username,
                PasswordHash = model.Password, // ⚠️ Plaintext — replace with hashed version in production
                Role = model.Role
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // ✅ 2. Save to Azure Table Storage via Azure Function
            var customer = new Customer
            {
                Username = model.Username,
                Name = model.Name,
                Surname = model.Surname,
                Email = model.Email,
                ShippingAddress = model.ShippingAddress
            };
            await _functionsApi.CreateCustomerAsync(customer);

            TempData["Success"] = "Account created successfully. Please login.";
            return RedirectToAction("Index", "Login");
        }
    }
}
