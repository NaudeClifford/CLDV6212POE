using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ABCRetails.Models;

namespace ABCRetails.Services
{
    public class FunctionApiClient : IFunctionApi
    {

        private readonly HttpClient _http;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        //Centrailze Funciton Routes
        private const string CustomersRoute = "customers";
        private const string ProductsRoute = "products";
        private const string OrdersRoute = "orders";
        private const string UploadsRoute = "uploads/proof-of-payment"; //multipart

        public FunctionApiClient(IHttpClientFactory factory)
        {
            _http = factory.CreateClient("Functions"); //BaseAddress in Program.cs
        }

        //Helpers

        private static HttpContent JsonBody(object obj)
            => new StringContent(JsonSerializer.Serialize(obj, _jsonOptions), Encoding.UTF8, "application/json");

        private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage resp)
        {
            resp.EnsureSuccessStatusCode();

            var stream = await resp.Content.ReadAsStreamAsync();

            var data = await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions);

            return data!;
        }

        //Customers

        public async Task<List<Customer>> GetCustomersAsync() =>
            await ReadJsonAsync<List<Customer>>(await _http.GetAsync(CustomersRoute));

        public async Task<Customer?> GetCustomerAsync(string id)
        {
            var resp = await _http.GetAsync($"{CustomersRoute}/{id}");

            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;

            return await ReadJsonAsync<Customer>(resp);
        }

        public async Task<Customer> CreateCustomerAsync(Customer c)
        {
            var content = JsonBody(new
            {
                name = c.Name,
                surname = c.Surname,
                username = c.Username,
                email = c.Email,
                shippingAddress = c.ShippingAddress
            });
            var resp = await _http.PostAsync(CustomersRoute, content);

            return await ReadJsonAsync<Customer>(resp);
        }

        public async Task<Customer> UpdateCustomerAsync(string id, Customer c)
        {
            var content = JsonBody(new
            {
                name = c.Name,
                surname = c.Surname,
                username = c.Username,
                email = c.Email,
                shippingAddress = c.ShippingAddress
            });

            var resp = await _http.PutAsync($"{CustomersRoute}/{id}", content);

            return await ReadJsonAsync<Customer>(resp);
        }

        public async Task DeleteCustomerAsync(string id)
        {
            var resp = await _http.DeleteAsync($"{CustomersRoute}/{id}");

            resp.EnsureSuccessStatusCode();
        }

        //Products
        public async Task<List<Product>> GetProductsAsync()
            => await ReadJsonAsync<List<Product>>(await _http.GetAsync(ProductsRoute));

        public async Task<Product?> GetProductAsync(string id)
        {
            var resp = await _http.GetAsync($"{ProductsRoute}/{id}");

            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;

            return await ReadJsonAsync<Product>(resp);
        }

        public async Task<Product> CreateProductAsync(Product p, IFormFile? imageFile)
        {
            using var form = new MultipartFormDataContent();

            form.Add(new StringContent(p.ProductName ?? string.Empty), nameof(p.ProductName));
            form.Add(new StringContent(p.Description ?? string.Empty), nameof(p.Description));
            form.Add(new StringContent(p.Price.ToString(CultureInfo.InvariantCulture)), nameof(p.Price));
            form.Add(new StringContent(p.StockAvailable.ToString()), nameof(p.StockAvailable));

            if (!string.IsNullOrWhiteSpace(p.ImageUrl))
                form.Add(new StringContent(p.ImageUrl), nameof(p.ImageUrl));

            if (imageFile is not null && imageFile.Length > 0)
            {
                var streamContent = new StreamContent(imageFile.OpenReadStream());
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType);
                form.Add(streamContent, "imageFile", imageFile.FileName);
            }

            var resp = await _http.PostAsync(ProductsRoute, form);

            return await ReadJsonAsync<Product>(resp);
        }

        public async Task<Product> UpdateProductAsync(string id, Product p, IFormFile? imageFile)
        {
            using var form = new MultipartFormDataContent();

            form.Add(new StringContent(p.ProductName ?? string.Empty), nameof(p.ProductName));
            form.Add(new StringContent(p.Description ?? string.Empty), nameof(p.Description));
            form.Add(new StringContent(p.Price.ToString(CultureInfo.InvariantCulture)), nameof(p.Price));
            form.Add(new StringContent(p.StockAvailable.ToString()), nameof(p.StockAvailable));

            if (!string.IsNullOrWhiteSpace(p.ImageUrl))
                form.Add(new StringContent(p.ImageUrl), nameof(p.ImageUrl));

            if (imageFile is not null && imageFile.Length > 0)
            {
                var streamContent = new StreamContent(imageFile.OpenReadStream());
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType);
                form.Add(streamContent, "imageFile", imageFile.FileName);
            }

            var resp = await _http.PutAsync($"{ProductsRoute}/{id}", form);

            return await ReadJsonAsync<Product>(resp);
        }

        public async Task DeleteProductAsync(string id)
        {
            var resp = await _http.DeleteAsync($"{ProductsRoute}/{id}");


            resp.EnsureSuccessStatusCode();
        }

        // Orders

        public async Task<List<Order>> GetOrdersAsync() =>
            await ReadJsonAsync<List<Order>>(await _http.GetAsync(OrdersRoute));

        public async Task<Order?> GetOrderAsync(string id)
        {
            var resp = await _http.GetAsync($"{OrdersRoute}/{id}");

            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;

            return await ReadJsonAsync<Order>(resp);
        }

        public async Task<Order> CreateOrderAsync(string customerId, string productId, int quantity)
        {
            var content = JsonBody(new
            {
                customerId,
                productId,
                quantity
            });

            var resp = await _http.PostAsync(OrdersRoute, content);

            return await ReadJsonAsync<Order>(resp);
        }

        public async Task UpdateOrderAsync(string id, string newStatus)
        {
            var content = JsonBody(new { status = newStatus });

            var resp = await _http.PutAsync($"{OrdersRoute}/{id}/status", content);

            resp.EnsureSuccessStatusCode();
        }

        public async Task DeleteOrderAsync(string id)
        {
            var resp = await _http.DeleteAsync($"{OrdersRoute}/{id}");

            resp.EnsureSuccessStatusCode();
        }

        // Uploads
        public async Task<string> UploadProofOfPaymentAsync(IFormFile file, string? orderId, string? customerId)
        {
            using var form = new MultipartFormDataContent();

            var streamContent = new StreamContent(file.OpenReadStream());

            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

            form.Add(streamContent, "proofOfPayment", file.FileName);

            if (!string.IsNullOrWhiteSpace(orderId))
                form.Add(new StringContent(orderId), "orderId");

            if (!string.IsNullOrWhiteSpace(customerId))
                form.Add(new StringContent(customerId), "customerId");

            var resp = await _http.PostAsync(UploadsRoute, form);

            resp.EnsureSuccessStatusCode();

            // Assume API returns URL or ID of uploaded file as plain string or JSON
            var result = await resp.Content.ReadAsStringAsync();

            return result;
        }

        public Task<List<Order>> GetOrderAsync()
        {
            throw new NotImplementedException();
        }
    }
}

