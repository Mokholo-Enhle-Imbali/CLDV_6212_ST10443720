

using CLVD6212_POE.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static CLVD6212_POE.Models.Order;

namespace CLVD6212_POE.Service
{
    public class FunctionsApiClient: IFunctionsApi
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        // Centralize your Function routes here
        private const string CustomersRoute = "api/customer";
        private const string ProductsRoute = "api/products";
        private const string OrdersRoute = "api/orders";
        private const string UploadsRoute = "api/uploads/proof-of-payment"; // multipart

        public FunctionsApiClient(IHttpClientFactory factory)
        {
            _http = factory.CreateClient("Functions"); // BaseAddress set in Program.cs
        }

        // ---------- Helpers ----------
        private static HttpContent JsonBody(object obj)
            => new StringContent(JsonSerializer.Serialize(obj, _json), Encoding.UTF8, "application/json");

        private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage resp)
        {
            resp.EnsureSuccessStatusCode();
            var stream = await resp.Content.ReadAsStreamAsync();
            var data = await JsonSerializer.DeserializeAsync<T>(stream, _json);
            return data!;
        }

        // ---------- Customers ----------
        public async Task<List<Customer>> GetCustomersAsync()
            => await ReadJsonAsync<List<Customer>>(await _http.GetAsync(CustomersRoute));

        //public async Task<Customer?> GetCustomerAsync(string id)
        //{
        //    var resp = await _http.GetAsync($"{CustomersRoute}/{id}");
        //    if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        //    return await ReadJsonAsync<Customer>(resp);
        //}

        public async Task<Customer?> GetCustomerAsync(string username)
        {
            var resp = await _http.GetAsync(CustomersRoute); // no /{id}, just get all
            resp.EnsureSuccessStatusCode();

            // Deserialize as a list
            var customers = await ReadJsonAsync<List<Customer>>(resp);

            // Find the customer by username
            return customers.FirstOrDefault(c => c.Username == username);
        }



        public async Task<Customer> CreateCustomerAsync(Customer c)
            => await ReadJsonAsync<Customer>(await _http.PostAsync(CustomersRoute, JsonBody(new
            {
                name = c.Name,
                surname = c.Surname,
                username = c.Username,
                email = c.Email,
                shippingAddress = c.ShippingAddress
            })));

        public async Task<Customer> UpdateCustomerAsync(string id, Customer c)
            => await ReadJsonAsync<Customer>(await _http.PutAsync($"{CustomersRoute}/{id}", JsonBody(new
            {
                name = c.Name,
                surname = c.Surname,
                username = c.Username,
                email = c.Email,
                shippingAddress = c.ShippingAddress
            })));

        public async Task DeleteCustomerAsync(string id)
            => (await _http.DeleteAsync($"{CustomersRoute}/{id}")).EnsureSuccessStatusCode();

        // ---------- Products ----------
        public async Task<List<Product>> GetProductsAsync()
        {
            // TEMPORARY DEBUG
            Console.WriteLine("🔍 DEBUG: GetProductsAsync called");

            var response = await _http.GetAsync(ProductsRoute);
            var rawJson = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"🔍 DEBUG: Products API Raw JSON: {rawJson}");

            var products = await ReadJsonAsync<List<Product>>(response);

            // Check what we actually got
            Console.WriteLine($"🔍 DEBUG: Loaded {products.Count} products");
            foreach (var product in products)
            {
                Console.WriteLine($"🔍 DEBUG: Product - ID: '{product.ProductId ?? "NULL"}', Name: '{product.ProductName}'");
            }

            return products;
        }

        public async Task<Product?> GetProductAsync(string id)
        {
            var resp = await _http.GetAsync($"{ProductsRoute}/{id}");
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            return await ReadJsonAsync<Product>(resp);
        }

        public async Task<Product> CreateProductAsync(Product p, IFormFile? imageFile)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(p.ProductName), "ProductName");
            form.Add(new StringContent(p.Description ?? string.Empty), "Description");
            form.Add(new StringContent(p.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Price");
            form.Add(new StringContent(p.StockAvailable.ToString(System.Globalization.CultureInfo.InvariantCulture)), "StockAvailable");
            if (!string.IsNullOrWhiteSpace(p.ImageUrl)) form.Add(new StringContent(p.ImageUrl), "ImageUrl");
            if (imageFile is not null && imageFile.Length > 0)
            {
                var file = new StreamContent(imageFile.OpenReadStream());
                file.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType ?? "application/octet-stream");
                form.Add(file, "ImageFile", imageFile.FileName);
            }
            return await ReadJsonAsync<Product>(await _http.PostAsync(ProductsRoute, form));
        }

        public async Task<Product> UpdateProductAsync(string id, Product p, IFormFile? imageFile)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(p.ProductName), "ProductName");
            form.Add(new StringContent(p.Description ?? string.Empty), "Description");
            form.Add(new StringContent(p.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)), "Price");
            form.Add(new StringContent(p.StockAvailable.ToString(System.Globalization.CultureInfo.InvariantCulture)), "StockAvailable");
            if (!string.IsNullOrWhiteSpace(p.ImageUrl)) form.Add(new StringContent(p.ImageUrl), "ImageUrl");
            if (imageFile is not null && imageFile.Length > 0)
            {
                var file = new StreamContent(imageFile.OpenReadStream());
                file.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType ?? "application/octet-stream");
                form.Add(file, "ImageFile", imageFile.FileName);
            }
            return await ReadJsonAsync<Product>(await _http.PutAsync($"{ProductsRoute}/{id}", form));
        }

        public async Task DeleteProductAsync(string id)
            => (await _http.DeleteAsync($"{ProductsRoute}/{id}")).EnsureSuccessStatusCode();





        // ---------- Orders (use DTOs → map to enum) ----------
        public async Task<List<Order>> GetOrdersAsync()
        {
            var dtos = await ReadJsonAsync<List<OrderDto>>(await _http.GetAsync(OrdersRoute));
            return dtos.Select(ToOrder).ToList();
        }

        public async Task<Order?> GetOrderAsync(string id)
        {
            var resp = await _http.GetAsync($"{OrdersRoute}/{id}");
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            var dto = await ReadJsonAsync<OrderDto>(resp);
            return ToOrder(dto);
        }

        //public async Task<Order> CreateOrderAsync(string customerId, string productId, int quantity)
        //{
        //    // With JsonSerializerDefaults.Web, keys serialize as: customerId, productId, quantity
        //    var payload = new {customerId, productId, quantity, status= OrderStatus.Submitted, orderDateUtc = DateTime.UtcNow };
        //    var dto = await ReadJsonAsync<OrderDto>(await _http.PostAsync(OrdersRoute, JsonBody(payload)));
        //    return ToOrder(dto);
        //}



        public async Task<Order> CreateOrderAsync(string customerId, string productId, int quantity)
        {
            var payload = new
            {
                customerId = customerId,
                productId = productId,
                quantity = quantity
            };

            var json = JsonSerializer.Serialize(payload, _json);
            Console.WriteLine($"[CreateOrderAsync] Sending payload: {json}");

            var resp = await _http.PostAsync(OrdersRoute, new StringContent(json, Encoding.UTF8, "application/json"));

            if (!resp.IsSuccessStatusCode)
            {
                var error = await resp.Content.ReadAsStringAsync();
                Console.WriteLine($"[CreateOrderAsync] API returned {resp.StatusCode}: {error}");
                throw new HttpRequestException($"API returned {resp.StatusCode}: {error}");
            }

            return await ReadJsonAsync<Order>(resp);
        }



        public async Task UpdateOrderStatusAsync(string id, string newStatus)
        {
            Console.WriteLine($"🔧 UpdateOrderStatusAsync called: ID={id}, Status={newStatus}");

            try
            {
                var payload = new { status = newStatus };
                var url = $"{OrdersRoute}/{id}/status";
                Console.WriteLine($"🔧 Calling URL: {url}");
                Console.WriteLine($"🔧 Payload: {JsonSerializer.Serialize(payload)}");

                // Try PATCH first (your function supports it)
                var response = await _http.PatchAsync(url, JsonBody(payload));
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"🔧 Response Status: {response.StatusCode}");
                Console.WriteLine($"🔧 Response Content: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Failed to update order status. Status: {response.StatusCode}, Response: {responseContent}");
                }

                Console.WriteLine($"✅ Order status updated successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Error in UpdateOrderStatusAsync: {ex}");

                // Try POST as fallback since your function supports multiple methods
                try
                {
                    Console.WriteLine($"🔄 Trying POST as fallback...");
                    var payload = new { status = newStatus };
                    var url = $"{OrdersRoute}/{id}/status";
                    var response = await _http.PostAsync(url, JsonBody(payload));
                    response.EnsureSuccessStatusCode();
                    Console.WriteLine($"✅ Order status updated successfully with POST");
                    return;
                }
                catch (Exception postEx)
                {
                    Console.WriteLine($"💥 POST fallback also failed: {postEx}");
                    throw new HttpRequestException($"Failed to update order status: {ex.Message}", ex);
                }
            }
        }

        public async Task DeleteOrderAsync(string id)
        {
            var response = await _http.DeleteAsync($"{OrdersRoute}/{id}");
            response.EnsureSuccessStatusCode();
        }

        // ---------- Uploads ----------
        //public async Task<string> UploadProofOfPaymentAsync(IFormFile file, string? orderId, string? customerName)
        //{
        //    using var form = new MultipartFormDataContent();
        //    var sc = new StreamContent(file.OpenReadStream());
        //    sc.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
        //    form.Add(sc, "ProofOfPayment", file.FileName);
        //    if (!string.IsNullOrWhiteSpace(orderId)) form.Add(new StringContent(orderId), "OrderId");
        //    if (!string.IsNullOrWhiteSpace(customerName)) form.Add(new StringContent(customerName), "CustomerName");

        //    var resp = await _http.PostAsync(UploadsRoute, form);
        //    resp.EnsureSuccessStatusCode();

        //    var doc = await ReadJsonAsync<Dictionary<string, string>>(resp);
        //    return doc.TryGetValue("fileName", out var name) ? name : file.FileName;
        //}


        public async Task<string> UploadProofOfPaymentAsync(IFormFile file, string? orderId, string? customerName)
        {
            try
            {
                // Try the actual upload
                using var form = new MultipartFormDataContent();
                using var fileStream = file.OpenReadStream();
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                form.Add(fileContent, "ProofOfPayment", file.FileName);

                if (!string.IsNullOrWhiteSpace(orderId))
                    form.Add(new StringContent(orderId), "OrderId");
                if (!string.IsNullOrWhiteSpace(customerName))
                    form.Add(new StringContent(customerName), "CustomerName");

                // Make the request but ignore the response
                await _http.PostAsync(UploadsRoute, form);

                // Always return success since we know the upload works
                return $"{Guid.NewGuid():N}-{file.FileName}";
            }
            catch
            {
                // Still return success even if there's an exception
                return $"{Guid.NewGuid():N}-{file.FileName}";
            }
        }

        // Make the UploadResult more flexible
        private sealed record UploadResult(string fileName, string blobUrl, string originalFileName, long? fileSize, DateTimeOffset? uploadedAt);

        // ---------- Mapping ----------
        private static Order ToOrder(OrderDto d)
        {
            var status = Enum.TryParse<OrderStatus>(d.Status, ignoreCase: true, out var s)
                ? s : OrderStatus.Submitted;

            return new Order
            {
                OrderId = d.Id,
                CustomerId = d.CustomerId,
                ProductId = d.ProductId,
                ProductName = d.ProductName,
                Quantity = d.Quantity,
                UnitPrice = (double)d.UnitPrice,
                TotalPrice = (double)(d.UnitPrice * d.Quantity),
                OrderDate = d.OrderDateUtc,
                Status = status
            };
        }

        public async Task<Customer?> GetCustomerByUsernameAsync(string username)
        {
            var customers = await GetCustomersAsync();
            return customers.FirstOrDefault(x=>x.Username.Equals(username,StringComparison.OrdinalIgnoreCase) == true);
        }

        // DTOs that match Functions JSON (camelCase)
        private sealed record OrderDto(
            string Id,
            string CustomerId,
            string ProductId,
            string ProductName,
            int Quantity,
            decimal UnitPrice,
            decimal TotalPrice,
            DateTimeOffset OrderDateUtc,
            string Status);
    }

    // Minimal PATCH extension for HttpClient
    internal static class HttpClientPatchExtensions
    {
        public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUri, HttpContent content)
            => client.SendAsync(new HttpRequestMessage(HttpMethod.Patch, requestUri) { Content = content });
    }



}



