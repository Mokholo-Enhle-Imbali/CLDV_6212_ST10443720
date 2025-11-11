using CLVD6212_POE.Data;
using CLVD6212_POE.Models;
using CLVD6212_POE.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static CLVD6212_POE.Models.Order;


namespace CLVD6212_POE.Controllers
{
    public class CartController : Controller
    {
        public readonly abcretailersDbContext _db;
        private readonly IFunctionsApi _api;

        public CartController(abcretailersDbContext db, IFunctionsApi api)
        {
            _db = db;
            _api = api;
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

        public async Task<IActionResult> Remove(string productId)
        {
            var customerId = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(customerId) || string.IsNullOrEmpty(productId))
                return RedirectToAction("Index");

            var existing = await _db.Cart.FirstOrDefaultAsync(c => c.ProductId == productId && c.CustomerUsername == customerId);

            if (existing != null)
                existing.Quantity -= 1;
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

        //public async Task<IActionResult> Checkout()
        //{
        //    var customerId = HttpContext.Session.GetString("Username");
        //    if (string.IsNullOrEmpty(customerId))
        //        return RedirectToAction("Index");

        //    var cartItems= await _db.Cart.Where(c=>c.CustomerUsername==customerId).ToListAsync();

        //    foreach (var item in cartItems)
        //    {
        //        var order = new Order
        //        {
        //            CustomerId = customerId,
        //            ProductId = item.ProductId,
        //            Quantity = item.Quantity,
        //            Status=OrderStatus.Submitted
        //        };
        //        _db.Orders.Add(order);
        //    }

        //    _db.Cart.RemoveRange(cartItems);
        //   await  _db.SaveChangesAsync();
        //   return RedirectToAction("Orders","Account");
        //}


        public async Task<IActionResult> Checkout()
        {
            // 1. Get the username from session
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index");

            // 2. Get the actual customer object from your API
            var customer = await _api.GetCustomerAsync(username);
            if (customer == null)
            {
                Console.WriteLine($"[Checkout Error] Customer not found for username '{username}'");
                return RedirectToAction("Index");
            }

            // 3. Get cart items from SQL
            var cartItems = await _db.Cart
                .Where(c => c.CustomerUsername == username)
                .ToListAsync();

            if (!cartItems.Any())
            {
                Console.WriteLine("[Checkout Info] Cart is empty");
                return RedirectToAction("Index");
            }

            // 4. Process each cart item into Azure Table order
            foreach (var item in cartItems)
            {
                try
                {
                

                   var response = await _api.CreateOrderAsync(customer.CustomerId, item.ProductId, item.Quantity); // your API call
                    //if (!response.IsSuccessStatusCode)
                    //{
                    //    Console.WriteLine($"[Checkout Error] Failed to create order for Product='{item.ProductId}': {response.ReasonPhrase}");
                    //    continue; // skip this item, continue with others
                    //}

                    Console.WriteLine($"[Checkout Info] Order created for Product='{item.ProductId}'");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Checkout Exception] {ex.Message}");
                }
            }

            //5.Optional: clear SQL cart
            _db.Cart.RemoveRange(cartItems);
            await _db.SaveChangesAsync();

            return RedirectToAction("MyOrders", "Order"); // or wherever you want
        }


    }
}
