using System.Text.Json;
using ABCRetailersFunctions.Entities;
using ABCRetailersFunctions.Helpers;
using ABCRetailersFunctions.Models;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Grpc.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ABCRetailersFunctions.Functions;

public class OrdersFunctions
{
    private readonly string _conn;
    private readonly string _ordersTable;
    private readonly string _productsTable;
    private readonly string _customersTable;
    private readonly string _queueOrder;
    private readonly string _queueStock;
    private readonly ILogger<OrdersFunctions> _logger;
    public OrdersFunctions(IConfiguration con, ILogger<OrdersFunctions> logger)
    {
        _conn = con["STORAGE_CONNECTION"] ?? throw new InvalidOperationException("STORAGE_CONNECTION missing");
        _ordersTable = con["TABLE_ORDER"] ?? "Order";
        _productsTable = con["TABLE_PRODUCT"] ?? "Product";
        _customersTable = con["TABLE_CUSTOMER"] ?? "Customer";
        _queueOrder = con["QUEUE_ORDER_NOTIFICATIONS"] ?? "order-notifications";
        _queueStock = con["QUEUE_STOCK_UPDATES"] ?? "stock-updates";

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Function("Orders_List")]
    public async Task<HttpResponseData> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders")] HttpRequestData req)
    {
        var ordersTable = new TableClient(_conn, _ordersTable);
        var customersTable = new TableClient(_conn, _customersTable);

        await ordersTable.CreateIfNotExistsAsync();
        await customersTable.CreateIfNotExistsAsync();

        var items = new List<OrderDto>();

        await foreach (var e in ordersTable.QueryAsync<OrderEntity>(x => x.PartitionKey == "Order"))
        {
            string customerUsername = "(Unknown)";

            try
            {
                var customer = await customersTable.GetEntityAsync<CustomerEntity>("Customer", e.CustomerId);
                customerUsername = customer.Value.Username;
            }
            catch
            {
                return await HttpJson.NotFound(req, "Customer not found");
            }

            var dto = Map.ToDto(e, customerUsername);
            items.Add(dto);
        }

        return await HttpJson.OK(req, items);
    }


    [Function("Orders_Get")]
    public async Task<HttpResponseData> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{id}")] HttpRequestData req,
        string id)
    {
        var table = new TableClient(_conn, _ordersTable);

        try
        {
            var entity = await table.GetEntityAsync<OrderEntity>("Order", id);
            
            var customers = new TableClient(_conn, _customersTable);
            string customerUsername = "(Unknown)";
            try
            {
                var customer = await customers.GetEntityAsync<CustomerEntity>("Customer", entity.Value.CustomerId);
                customerUsername = customer.Value.Username;
                Console.WriteLine("UserName: " + customerUsername);

            }
            catch {
                return await HttpJson.NotFound(req, "Customer not found");
            }

            return await HttpJson.OK(req, Map.ToDto(entity.Value, customerUsername));
        }
        catch
        {
            return await HttpJson.NotFound(req, "Order not found");
        }
    }

    private record OrderCreate(string CustomerId, string ProductId, int Quantity);

    [Function("Orders_Create")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")] HttpRequestData req)
    {

        var input = await HttpJson.ReadAsync<OrderCreate>(req);
        if (input == null || string.IsNullOrWhiteSpace(input.CustomerId) || string.IsNullOrWhiteSpace(input.ProductId) || input.Quantity < 1) 
            return await HttpJson.Bad(req, "CustomerId, ProductId, Quantity >= 1 required");

        var orders = new TableClient(_conn, _ordersTable);
        var products = new TableClient(_conn, _productsTable);
        var customers = new TableClient(_conn, _customersTable);
        await orders.CreateIfNotExistsAsync();
        await products.CreateIfNotExistsAsync();
        await customers.CreateIfNotExistsAsync();

        ProductEntity product;
        CustomerEntity customer;
        
        try
        {
            product = (await products.GetEntityAsync<ProductEntity>("Product", input.ProductId)).Value;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogWarning(ex, "Failed to get product with ID {ProductId}", input.ProductId);
            return await HttpJson.Bad(req, "Invalid ProductId");
        }

        try
        {
            customer = (await customers.GetEntityAsync<CustomerEntity>("Customer", input.CustomerId)).Value;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogWarning(ex, "Failed to get customer with ID {CustomerId}", input.CustomerId);
            return await HttpJson.Bad(req, "Invalid CustomerId");
        }

        if (product.StockAvailable < input.Quantity)
            return await HttpJson.Bad(req, $"Insufficient stock. Available: {product.StockAvailable}");

        var order = new OrderEntity
        {
            CustomerId = input.CustomerId,
            ProductId = input.ProductId,
            ProductName = product.ProductName,
            Quantity = input.Quantity,
            UnitPrice = product.Price,
            OrderDateUtc = DateTimeOffset.UtcNow,
            Status = "Submitted"

        };
        await orders.AddEntityAsync(order);

        product.StockAvailable -= input.Quantity;
        await products.UpdateEntityAsync(product, product.ETag, TableUpdateMode.Replace);

        //Send queue messages
        var queueOrder = await GetQueueClientAsync(_queueOrder);
        var queueStock = await GetQueueClientAsync(_queueStock);

        await queueOrder.CreateIfNotExistsAsync();
        await queueStock.CreateIfNotExistsAsync();

        var orderMsg = new
        {
            Type = "OrderCreated",
            OrderId = order.RowKey,
            order.CustomerId,
            CustomerName = $"{customer.Name} {customer.Surname}",
            order.ProductId,
            product.ProductName,
            order.Quantity,
            order.UnitPrice,
            TotalAmount = order.UnitPrice * order.Quantity,
            order.OrderDateUtc,
            order.Status
        };
        _logger.LogInformation("Sending message to queue: OrderCreated");
        await queueOrder.SendMessageAsync(JsonSerializer.Serialize(orderMsg));
        _logger.LogInformation("OrderCreated message sent.");

        var stockMsg = new
        {
            Type = "StockUpdated",
            productId = product.RowKey,
            product.ProductName,
            PreviousStock = product.StockAvailable + input.Quantity,
            NewStock = product.StockAvailable,
            UpdatedDateUtc = DateTimeOffset.UtcNow,
            UpdatedBy = "Order System"
        };
        _logger.LogInformation("Sending message to queue: StockUpdate");
        await queueStock.SendMessageAsync(JsonSerializer.Serialize(stockMsg));
        _logger.LogInformation("StockUpdated message sent.");

        return await HttpJson.Created(req, Map.ToDto(order, customer.Username));
    }

    private record OrderStatusUpdate(string Status);

    [Function("Orders_UpdateStatus")]
    public async Task<HttpResponseData> UpdateStatus(
    [HttpTrigger(AuthorizationLevel.Anonymous, "patch","post", "put", Route = "orders/{id}/status")] HttpRequestData req,
    string id)
    {
        var input = await HttpJson.ReadAsync<OrderStatusUpdate>(req);

        if (input == null || string.IsNullOrWhiteSpace(input.Status))
            return await HttpJson.Bad(req, "Status is required");

        var orders = new TableClient(_conn, _ordersTable);

        try
        {
            var response = await orders.GetEntityAsync<OrderEntity>("Order", id);
            var entity = response.Value;
            var previous = entity.Status;

            entity.Status = input.Status;
            await orders.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);
            
            var queueOrder = await GetQueueClientAsync(_queueOrder);


            var statusMsg = new
            {

                Type = "OrderStatusUpdated",
                OrderId = entity.RowKey,
                PreviousStatus = previous,
                NewStatus = entity.Status,
                UpdatedDateUtc = DateTimeOffset.UtcNow,
                UpdatedBy = "System"
            };
            _logger.LogInformation("Sending message to queue: statusMsg");
            await queueOrder.SendMessageAsync(JsonSerializer.Serialize(statusMsg));
            _logger.LogInformation("statusMsg message sent.");

            var customers = new TableClient(_conn, _customersTable);
            string customerUsername = "(Unknown)";
            try
            {
                var customer = await customers.GetEntityAsync<CustomerEntity>("Customer", entity.CustomerId);
                customerUsername = customer.Value.Username;
            }
            catch {
                return await HttpJson.NotFound(req, "Customer not found");
            }

            return await HttpJson.OK(req, Map.ToDto(entity, customerUsername));
        }
        catch
        { 
            return await HttpJson.NotFound(req, "Order not found");
        }
    }

    [Function("Orders_Delete")]
    public async Task<HttpResponseData> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "orders/{id}")] HttpRequestData req, string id)
    {
        var table = new TableClient(_conn, _ordersTable);
        await table.DeleteEntityAsync("Order", id);
        return await HttpJson.NoContent(req);
    }

    private async Task<QueueClient> GetQueueClientAsync(string queueName)
    {
        var client = new QueueClient(_conn, queueName, new QueueClientOptions
        {
            MessageEncoding = QueueMessageEncoding.Base64
        });

        await client.CreateIfNotExistsAsync();
        return client;
    }

}