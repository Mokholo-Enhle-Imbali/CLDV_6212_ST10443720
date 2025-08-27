using CLVD6212_POE.Models;
using CLVD6212_POE.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Reflection;
using System.Text.Json;

namespace CLVD6212_POE.Controllers
{
    public class OrderController : Controller
    {

        private readonly IAzureStorageService _storageService;

        public OrderController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }


        public async Task<IActionResult> Index()
        {
            var order = await _storageService.GetAllEntitiesAsync<Order>();
            return View(order);
        }


        //Create order view and http post (funcrional part)
        public async Task<IActionResult> Create()
        {
            var customers = await _storageService.GetAllEntitiesAsync<Customer>();
            var products = await _storageService.GetAllEntitiesAsync<Product>();

            var viewModel = new OrderCreateViewModel
            {

                Customers= customers,
                Products= products
            };

            return View(viewModel);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {

                    var product= await _storageService.GetEntityAsync<Product>("Product",viewModel.ProductId);
                    var customer = await _storageService.GetEntityAsync<Customer>("Customer", viewModel.CustomerId);
                    if (customer == null|| product == null)
                    {
                        ModelState.AddModelError("", "Invalid customer or product selected");
                        await PopulateDropdowns(viewModel);
                        return View(viewModel);
                    }

                    if (product.StockAvailable<viewModel.Quantitiy)
                    {
                        ModelState.AddModelError("", $"Cannot order {viewModel.Quantitiy} units. Only {product.StockAvailable} in stock.");
                        await PopulateDropdowns(viewModel);
                        return View(viewModel);
                    }

                    viewModel.OrderDate = DateTime.SpecifyKind(viewModel.OrderDate, DateTimeKind.Utc);

                    //creation of an order
                    var order = new Order()
                    {
                        CustomerId = viewModel.CustomerId,
                        Username = customer.Username,
                        ProductId = viewModel.ProductId,
                        ProductName = product.ProductName,
                        OrderDate = viewModel.OrderDate,
                        Quantitiy = viewModel.Quantitiy,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price * viewModel.Quantitiy,
                        Status = "Submitted"
                    };

                    await _storageService.AddEntityAsync(order);

                    //update amount of stock
                    product.StockAvailable-=viewModel.Quantitiy;
                    await _storageService.UpdateEntityAsync(product);

                    //send queue message for a new order
                    var orderMessage = new
                    {
                        ProductId= product.ProductId,
                        ProductName=product.ProductName,
                        PreviousStock= product.StockAvailable + viewModel.Quantitiy,
                        NewStock= product.StockAvailable,
                        UpdatedBy= "Order System",
                        UpdateDate= DateTime.UtcNow
                    };

                    await _storageService.SendMessageAsync("stock-updates", JsonSerializer.Serialize(orderMessage));

                    TempData["Sucess Message"] = $"Order Created Sucessfully";
                    return RedirectToAction(nameof(Index));
                }

                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating the Order: {ex.Message}");

                }

            }

            await PopulateDropdowns(viewModel);
            return View(viewModel);
        }


        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) 
                return NotFound();

            var order= await _storageService.GetEntityAsync<Order>("Order",id);
            if (order == null)
                return NotFound();

            return View(order);
        }


        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var order = await _storageService.GetEntityAsync<Order>("Order", id);
            if (order == null)
                return NotFound();

           
            var customer = await _storageService.GetEntityAsync<Customer>("Customer", order.CustomerId);
            var product = await _storageService.GetEntityAsync<Product>("Product", order.ProductId);

        
            order.Username = customer?.Username ?? "Unknown Customer";
            order.ProductName = product?.ProductName ?? "Unknown Product";

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var originalOrder = await _storageService.GetEntityAsync<Order>("Order", order.RowKey);
                    if (originalOrder == null)
                    {
                        ModelState.AddModelError("", "Order not found in database");
                        return View(order);
                    }

                    // Update ONLY the fields that should be changed
                    originalOrder.OrderDate = DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc);
                    originalOrder.Status = order.Status;
                    originalOrder.ETag = order.ETag;


                    await _storageService.UpdateEntityAsync(originalOrder);
                    TempData["Success"] = "Order Updated Successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating the order: {ex.Message}");
                }
            }

            // refill readonly fields for display
            var customer = await _storageService.GetEntityAsync<Customer>("Customer", order.CustomerId);
            var product = await _storageService.GetEntityAsync<Product>("Product", order.ProductId);
            order.Username = customer?.Username ?? "Unknown Customer";
            order.ProductName = product?.ProductName ?? "Unknown Product";

            return View(order);
        }



        public async Task<IActionResult> Delete(string partitionKey, string rowKey) //get the view
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            var entity = await _storageService.GetEntityAsync<Order>("Order", rowKey);

            if (entity == null)
            {
                return NotFound();
            }

            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _storageService.DeleteEntityAsync<Order>("Order", id);
                TempData["Success"] = "Order Deleted Successfully";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error Deleting the order {ex.Message}"; 
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            try
            {
                var product = await _storageService.GetEntityAsync<Product>("Product", productId);
                if (product !=null)
                {
                    return Json(new
                    {
                        Success= true,
                        Price=product.Price,
                        Stock= product.StockAvailable,
                        ProductName= product.ProductName
                    });
                }

                return Json(new { Success = false });
            }
            catch 
            {
                return Json(new { Success = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(string id, string newStatus)
        {
            try
            {
                var order = await _storageService.GetEntityAsync<Order>("Order", id);
                if (order == null)
                {
                    return Json(new { Success = false, Message= "Order Not found"});
                }



                var previousStatus= order.Status;
                order.Status = newStatus;

                order.OrderDate = DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc);

                await _storageService.UpdateEntityAsync(order);

                //send queue message for status update

                var statusMessage = new
                {
                    OrderId= order.OrderId,
                    CustomerId= order.CustomerId,
                    CustomerName= order.Username,
                    ProductName = order.ProductName,
                    PreviousStatus= previousStatus,
                    NewStatus= newStatus,
                    UpdatedDate= DateTime.UtcNow,
                    UpdatedBy= "System"
                };

                await _storageService.SendMessageAsync("order-notifications", JsonSerializer.Serialize(statusMessage));

                return Json(new { Success = true, Message= $"Order status updated to {newStatus}"});

            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = ex.Message });
            }
        }



        private async Task PopulateDropdowns(OrderCreateViewModel model)
        {
            model.Customers= await _storageService.GetAllEntitiesAsync<Customer>();
            model.Products= await _storageService.GetAllEntitiesAsync<Product>();
        }


    }
}
