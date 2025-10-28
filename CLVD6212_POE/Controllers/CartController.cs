using CLVD6212_POE.Data;
using CLVD6212_POE.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using static CLVD6212_POE.Models.Order;


namespace CLVD6212_POE.Controllers
{
    public class CartController : Controller
    {
        public readonly abcretailersDbContext _db;

        public CartController(abcretailersDbContext db)
        {
            _db = db;
        }
        public async Task<IActionResult> Index()
        {
            var customerId = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(customerId))
            {
                return RedirectToAction("Index","Login");
            }

            var items = await _db.Cart.Where(c => c.CustomerUsername == customerId).ToListAsync();
            return View(items);
        }
        
        public async Task<IActionResult> Add(string productId)
        {
            var customerId = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(customerId) || string.IsNullOrEmpty(productId))
                return RedirectToAction("Index");

            var existing= await _db.Cart.FirstOrDefaultAsync(c=>c.ProductId == productId && c.CustomerUsername==customerId);

            if (existing != null)
                existing.Quantity += 1;
            else
            {
                _db.Cart.Add(new Cart
                {
                    CustomerUsername = customerId,
                    ProductId = productId,
                    Quantity = 1
                });
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Checkout()
        {
            var customerId = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(customerId))
                return RedirectToAction("Index");

            var cartItems= await _db.Cart.Where(c=>c.CustomerUsername==customerId).ToListAsync();

            foreach (var item in cartItems)
            {
                var order = new Order
                {
                    CustomerId = customerId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Status=OrderStatus.Submitted
                };
                _db.Orders.Add(order);
            }

            _db.Cart.RemoveRange(cartItems);
           await  _db.SaveChangesAsync();
           return RedirectToAction("Orders","Account");
        }
    }
}
