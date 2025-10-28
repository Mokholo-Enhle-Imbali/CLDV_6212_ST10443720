using ABCRetailersFunction.Entities;
using ABCRetailersFunction.Helpers;
using ABCRetailersFunction.Models;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ABCRetailersFunction.Functions
{
    public class OrderFunctions
    {
        private readonly string _conn;
        private readonly string _ordersTable;
        private readonly string _productsTable;
        private readonly string _customersTable;
        private readonly string _queueOrder;
        private readonly string _queueStock;

        public OrderFunctions(IConfiguration cfg)
        {
            _conn = cfg["STORAGE_CONNECTION"] ?? throw new InvalidOperationException("STORAGE_CONNECTION missing");
            _ordersTable = cfg["TABLE_ORDER"] ?? "Order";
            _productsTable = cfg["TABLE_PRODUCT"] ?? "Product";
            _customersTable = cfg["TABLE_CUSTOMER"] ?? "Customer";
            _queueOrder = cfg["QUEUE_ORDER_NOTIFICATIONS"] ?? "order-notifications";
            _queueStock = cfg["QUEUE_STOCK_UPDATES"] ?? "stock-updates";
        }

        //List of orders
        [Function("OrderList")]
        public async Task<HttpResponseData> List(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders")] HttpRequestData req)
        {
            var table = new TableClient(_conn, _ordersTable);
            await table.CreateIfNotExistsAsync();

            var items = new List<OrderDto>();
            await foreach (var e in table.QueryAsync<OrderEntity>(x => x.PartitionKey == "Order"))
                items.Add(Map.ToDto(e));

           
            var ordered = items.OrderByDescending(o => o.OrderDateUtc).ToList();
            return await HttpJson.OkAsync(req, ordered);
        }

        [Function("OrderGet")]
        public async Task<HttpResponseData> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{id}")] HttpRequestData req, string id)
        {
            var table = new TableClient(_conn, _ordersTable);
            try
            {
                var e = await table.GetEntityAsync<OrderEntity>("Order", id);
                return await HttpJson.OkAsync(req, Map.ToDto(e.Value));
            }
            catch
            {
                return await HttpJson.NotFoundAsync(req, "Order not found");
            }
        }

        [Function("Order_GetOrderByCustomerId")]
        public async Task<HttpResponseData> GetOrdersByCustomerId
            (
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route ="orders/by-customer/{customerId}")] HttpRequestData req, string customerId
            )
        {
            var table= new TableClient(_conn, _ordersTable);
            await table.CreateIfNotExistsAsync();

            var orders= new List<OrderDto>();
            await foreach(var entity in table.QueryAsync<OrderEntity>(x=>x.PartitionKey=="Order" && x.CustomerId == customerId))
            {
                orders.Add(Map.ToDto(entity));
            }
            return HttpJson.Ok(req, orders.OrderByDescending(o => o.OrderDateUtc).ToList());
        }

        public record OrderCreate(string CustomerId, string ProductId, int Quantity);

        [Function("OrderCreate")]
        public async Task<HttpResponseData> Create([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")] HttpRequestData req)
        {
            try
            {
                var input = await HttpJson.ReadAsync<OrderCreate>(req);
                if (input is null || string.IsNullOrWhiteSpace(input.CustomerId) || string.IsNullOrWhiteSpace(input.ProductId) || input.Quantity < 1)
                    return await HttpJson.BadAsync(req, "CustomerId, ProductId, Quantity >= 1 required");

                Console.WriteLine($"🔍 DEBUG: OrderCreate - CustomerId: {input.CustomerId}, ProductId: {input.ProductId}, Quantity: {input.Quantity}");

                var orders = new TableClient(_conn, _ordersTable);
                var products = new TableClient(_conn, _productsTable);
                var customers = new TableClient(_conn, _customersTable);

                await orders.CreateIfNotExistsAsync();
                await products.CreateIfNotExistsAsync();
                await customers.CreateIfNotExistsAsync();

                Console.WriteLine("🔍 DEBUG: Tables verified");

                ProductEntity product;
                CustomerEntity customer;

                try
                {
                    Console.WriteLine($"🔍 DEBUG: Getting product: {input.ProductId}");
                    product = (await products.GetEntityAsync<ProductEntity>("Product", input.ProductId)).Value;
                    Console.WriteLine($"🔍 DEBUG: Product found: {product.ProductName}, Stock: {product.StockAvailable}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"🔍 DEBUG: Product error: {ex.Message}");
                    return await HttpJson.BadAsync(req, $"Invalid ProductId: {input.ProductId}");
                }

                try
                {
                    Console.WriteLine($"🔍 DEBUG: Getting customer: {input.CustomerId}");
                    customer = (await customers.GetEntityAsync<CustomerEntity>("Customer", input.CustomerId)).Value;
                    Console.WriteLine($"🔍 DEBUG: Customer found: {customer.Name} {customer.Surname}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"🔍 DEBUG: Customer error: {ex.Message}");
                    return await HttpJson.BadAsync(req, $"Invalid CustomerId: {input.CustomerId}");
                }

                if (product.StockAvailable < input.Quantity)
                {
                    Console.WriteLine($"🔍 DEBUG: Insufficient stock: {product.StockAvailable} < {input.Quantity}");
                    return await HttpJson.BadAsync(req, $"Insufficient stock. Available: {product.StockAvailable}");
                }

                Console.WriteLine("🔍 DEBUG: Creating order entity...");

                var order = new OrderEntity
                {
                    CustomerId = input.CustomerId,
                    ProductId = input.ProductId,
                    ProductName = product.ProductName,
                    Quantitiy = input.Quantity,
                    UnitPrice = product.Price,
                    OrderDate = DateTimeOffset.UtcNow,
                    Status = "Submitted"
                };

                Console.WriteLine("🔍 DEBUG: Adding order to table...");
                await orders.AddEntityAsync(order);
                Console.WriteLine($"🔍 DEBUG: Order created with ID: {order.RowKey}");

                // UPDATE STOCK
                product.StockAvailable -= input.Quantity;
                await products.UpdateEntityAsync(product, product.ETag, TableUpdateMode.Replace);
                Console.WriteLine($"🔍 DEBUG: Stock updated: {product.StockAvailable}");

                // SEND TO QUEUES - ADD THIS SECTION
                await SendToQueue(_queueOrder, JsonSerializer.Serialize(new
                {
                    Type = "OrderCreated",
                    OrderId = order.RowKey,
                    CustomerId = order.CustomerId,
                    ProductId = order.ProductId,
                    ProductName = order.ProductName,
                    Quantity = order.Quantitiy,
                    UnitPrice = order.UnitPrice,
                    TotalAmount = order.UnitPrice * order.Quantitiy,
                    OrderDate = order.OrderDate,
                    Status = order.Status
                }));

                await SendToQueue(_queueStock, JsonSerializer.Serialize(new
                {
                    Type = "StockReduced",
                    ProductId = product.RowKey,
                    ProductName = product.ProductName,
                    PreviousStock = product.StockAvailable + input.Quantity,
                    NewStock = product.StockAvailable,
                    Change = -input.Quantity,
                    Reason = "OrderCreated",
                    OrderId = order.RowKey
                }));

                return await HttpJson.CreatedAsync(req, Map.ToDto(order));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔍 DEBUG: OrderCreate overall error: {ex}");
                return await HttpJson.BadAsync(req, $"Internal error: {ex.Message}");
            }
        }
        public record OrderStatusUpdate(string Status);

        [Function("Orders_UpdateStatus")]
        public async Task<HttpResponseData> UpdateStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", "post", "put", Route = "orders/{id}/status")] HttpRequestData req, string id)
        {
            var input = await HttpJson.ReadAsync<OrderStatusUpdate>(req);
            if (input is null || string.IsNullOrWhiteSpace(input.Status))
                return await HttpJson.BadAsync(req, "Status is required");

            var orders = new TableClient(_conn, _ordersTable);
            try
            {
                var resp = await orders.GetEntityAsync<OrderEntity>("Order", id);
                var e = resp.Value;
                var previous = e.Status;

                e.Status = input.Status;
                await orders.UpdateEntityAsync(e, e.ETag, TableUpdateMode.Replace);


                var queueOrder = new QueueClient(_conn, _queueOrder, new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
                await queueOrder.CreateIfNotExistsAsync();
                var statusMsg = new
                {
                    Type = "OrderStatusUpdated",
                    OrderId = e.RowKey,
                    PreviousStatus = previous,
                    NewStatus = e.Status,
                    UpdatedDateUtc = DateTimeOffset.UtcNow,
                    UpdatedBy = "System"
                };
                await queueOrder.SendMessageAsync(JsonSerializer.Serialize(statusMsg));

                return await HttpJson.OkAsync(req, Map.ToDto(e));
            }
            catch
            {
                return await HttpJson.NotFoundAsync(req, "Order not found");
            }
        }
        [Function("OrderDelete")]
        public async Task<HttpResponseData> Delete(
    [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "orders/{id}")] HttpRequestData req, string id)
        {
            try
            {
                var table = new TableClient(_conn, _ordersTable);

                // First, get the order to find its partition key
                var order = await table.GetEntityAsync<TableEntity>("Order", id);
                if (order != null)
                {
                    // Delete using the correct partition key and row key
                    await table.DeleteEntityAsync(order.Value.PartitionKey, id);
                    return HttpJson.NoContent(req);
                }
                else
                {
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }
            }
            catch (Exception ex)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
                return errorResponse;
            }
        }


        private async Task SendToQueue(string queueName, string message)
        {
            try
            {
                var queueClient = new QueueClient(_conn, queueName, new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
                await queueClient.CreateIfNotExistsAsync();
                await queueClient.SendMessageAsync(message);
                Console.WriteLine($"✅ Sent to {queueName}: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to send to {queueName}: {ex.Message}");
            }
        }
    }
}
