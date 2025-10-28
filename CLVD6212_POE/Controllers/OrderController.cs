using CLVD6212_POE.Models;
using CLVD6212_POE.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;

namespace CLVD6212_POE.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {

        private readonly IFunctionsApi _api;
        public OrderController(IFunctionsApi api) => _api = api;

        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> Manage()
        {
            var orders= await _api.GetOrdersAsync();
            return View(orders.OrderByDescending(o=>o.OrderDate).ToList());
        }

        // LIST
        [Authorize(Roles ="Admin,Customer")]
        public async Task<IActionResult> Index()
        {
            var orders = await _api.GetOrdersAsync();
            return View(orders.OrderByDescending(o => o.OrderDate).ToList());
        }






















        // CREATE (GET)
        public async Task<IActionResult> Create()
        {
            var customers = await _api.GetCustomersAsync();
            var products = await _api.GetProductsAsync();

            var vm = new OrderCreateViewModel
            {
                Customers = customers,
                Products = products,
                OrderDate = DateTime.Today // Set default order date to today
            };

            // Check user role and set appropriate view data
            if (User.IsInRole("Admin"))
            {
                ViewData["ShowCustomerDropdown"] = true;
            }
            else if (User.IsInRole("Customer"))
            {
                // Customer can only create orders for themselves
                var currentCustomerId = GetCurrentCustomerId();

                if (string.IsNullOrEmpty(currentCustomerId))
                {
                    TempData["Error"] = "Unable to determine customer account. Please contact administrator.";
                    return RedirectToAction(nameof(Index));
                }

                // Pre-populate with current customer
                vm.CustomerId = currentCustomerId;
                ViewData["ShowCustomerDropdown"] = false;
                ViewData["CurrentCustomerId"] = currentCustomerId;

                // Get customer name for display
                var currentCustomer = customers.FirstOrDefault(c => c.CustomerId == currentCustomerId);
                if (currentCustomer != null)
                {
                    ViewData["CurrentCustomerName"] = $"{currentCustomer.Name} {currentCustomer.Surname} ({currentCustomer.Username})";
                }
                else
                {
                    ViewData["CurrentCustomerName"] = currentCustomerId;
                }
            }

            return View(vm);
        }









        // CREATE (POST)
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer,Admin")] // Allow both roles
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            // If user is Customer, force their own customer ID
            if (User.IsInRole("Customer"))
            {
                var currentCustomerId = GetCurrentCustomerId();

                if (string.IsNullOrEmpty(currentCustomerId))
                {
                    TempData["Error"] = "Unable to determine customer account. Please contact administrator.";
                    return RedirectToAction(nameof(Index));
                }

                // Force the customer ID to be the current user's ID
                model.CustomerId = currentCustomerId;

                // Remove ModelState error for CustomerId since we're setting it manually
                ModelState.Remove("CustomerId");
            }

            // Set default status if not set
            if (string.IsNullOrEmpty(model.Status))
            {
                model.Status = "Submitted";
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);

                // Re-set the view data based on user role
                SetCreateViewData(User.IsInRole("Admin"), GetCurrentCustomerId(), model.Customers);
                return View(model);
            }

            try
            {
                // Create order via Function
                var saved = await _api.CreateOrderAsync(model.CustomerId, model.ProductId, model.Quantity);

                TempData["Success"] = "Order created successfully!";

                // FIX: Redirect based on user role
                if (User.IsInRole("Customer"))
                {
                    return RedirectToAction(nameof(MyOrders)); // Customers go to MyOrders
                }
                else
                {
                    return RedirectToAction(nameof(Index)); // Admins go to Index
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error creating order: {ex.Message}");
                await PopulateDropdowns(model);

                // Re-set the view data based on user role
                SetCreateViewData(User.IsInRole("Admin"), GetCurrentCustomerId(), model.Customers);
                return View(model);
            }
        }






















        // Helper method to set view data for Create action
        private void SetCreateViewData(bool isAdmin, string currentCustomerId, List<Customer> customers)
        {
            if (isAdmin)
            {
                ViewData["ShowCustomerDropdown"] = true;
            }
            else
            {
                ViewData["ShowCustomerDropdown"] = false;
                ViewData["CurrentCustomerId"] = currentCustomerId;

                var currentCustomer = customers?.FirstOrDefault(c => c.CustomerId == currentCustomerId);
                if (currentCustomer != null)
                {
                    ViewData["CurrentCustomerName"] = $"{currentCustomer.Name} {currentCustomer.Surname} ({currentCustomer.Username})";
                }
                else
                {
                    ViewData["CurrentCustomerName"] = currentCustomerId;
                }
            }
        }















        // Helper method to get current customer ID
        private string GetCurrentCustomerId()
        {
            // Method 1: If your customer ID is stored in claims
            var customerIdClaim = User.FindFirst("CustomerId") ??
                                 User.FindFirst(ClaimTypes.NameIdentifier) ??
                                 User.FindFirst(ClaimTypes.Name);

            if (customerIdClaim != null)
            {
                return customerIdClaim.Value;
            }

            // Method 2: If username = customerId
            var userName = User.Identity.Name;
            if (!string.IsNullOrEmpty(userName))
            {
                return userName;
            }

            return null;
        }










        // Helper method to get current customer ID
       
        // DETAILS
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var order = await _api.GetOrderAsync(id);
            return order is null ? NotFound() : View(order);
        }

        // EDIT (GET) - typically only status is editable
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var order = await _api.GetOrderAsync(id);
            return order is null ? NotFound() : View(order);
        }

        // EDIT (POST) - status only
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> Edit(Order posted)
        {
            if (!ModelState.IsValid) return View(posted);

            try
            {
                await _api.UpdateOrderStatusAsync(posted.OrderId, posted.Status.ToString());
                TempData["Success"] = "Order updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error updating order: {ex.Message}");
                return View(posted);
            }
        }












        // DELETE
        [HttpPost]
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _api.DeleteOrderAsync(id);
                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }

            // Force redirect to refresh data
            return RedirectToAction(nameof(Index));
        }










        // AJAX: price/stock lookup
        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            try
            {
                var product = await _api.GetProductAsync(productId);
                if (product is not null)
                {
                    return Json(new
                    {
                        success = true,
                        price = product.Price,
                        stock = product.StockAvailable,
                        productName = product.ProductName
                    });
                }
                return Json(new { success = false });
            }
            catch
            {
                return Json(new { success = false });
            }
        }












        // AJAX: status update
        [HttpPost]
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> UpdateOrderStatus(string id, string newStatus)
        {
            try
            {
                await _api.UpdateOrderStatusAsync(id, newStatus);
                return Json(new { success = true, message = $"Order status updated to {newStatus}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task PopulateDropdowns(OrderCreateViewModel model)
        {
            model.Customers = await _api.GetCustomersAsync();
            model.Products = await _api.GetProductsAsync();
        }















        //for customer
        [Authorize(Roles = "Admin,Customer")]
        public async Task<IActionResult> MyOrders()
        {
            var orders = await _api.GetOrdersAsync();

            // If user is a customer, only show their orders
            if (User.IsInRole("Customer"))
            {
                var currentCustomerId = GetCurrentCustomerId();
                if (!string.IsNullOrEmpty(currentCustomerId))
                {
                    orders = orders.Where(o => o.CustomerId == currentCustomerId)
                                  .OrderByDescending(o => o.OrderDate)
                                  .ToList();
                    ViewData["PageTitle"] = "My Orders";
                }
            }
            else
            {
                // Admin sees all orders
                orders = orders.OrderByDescending(o => o.OrderDate).ToList();
                ViewData["PageTitle"] = "All Orders";
            }

            return View(orders);
        }









    }
}
