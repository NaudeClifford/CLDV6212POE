using System.Text.Json;
using ABCRetailersFunctions.Entities;
using ABCRetailersFunctions.Helpers;
using ABCRetailersFunctions.Models;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using static ABCRetailersFunctions.Functions.CustomersFunctions;

namespace ABCRetailersFunctions.Functions;

public class OrdersFunctions
{
    private readonly string _conn;
    private readonly string _ordersTable;
    private readonly string _productsTable;
    private readonly string _customersTable;
    private readonly string _queueOrder;
    private readonly string _queueStock;

    public OrdersFunctions(IConfiguration con)
    {
        _conn = con["STORAGE_CONNECTION"] ?? throw new InvalidOperationException("STORAGE_CONNECTION missing");
        _ordersTable = con["TABLE_ORDER"] ?? "Order";
        _productsTable = con["TABLE_PRODUCT"] ?? "Product";
        _customersTable = con["TABLE_CUSTOMER"] ?? "Customer";
        _queueOrder = con["QUEUE_ORDER_NOTIFICATIONS"] ?? "order-notifications";
        _queueStock = con["QUEUE_STOCK_UPDATES"] ?? "stock-updates";
    }

    [Function("Orders_List")]
    public async Task<HttpResponseData> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders")] HttpRequestData req)
    {
        var table = new TableClient(_conn, _ordersTable);
        await table.CreateIfNotExistsAsync();

        var items = new List<OrderDto>();
        await foreach (var e in table.QueryAsync<OrderEntity>(x => x.PartitionKey == "Order"))
            items.Add(Map.ToDto(e));

        var ordered = items.OrderByDescending(o => o.OrderDateUtc).ToList();
        return HttpJson.OK(req, ordered);
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
            return HttpJson.OK(req, Map.ToDto(entity.Value));
        }
        catch
        {
            return HttpJson.NotFound(req, "Order not found");
        }
    }

    private record OrderCreate(string CustomerId, string ProductId, int Quantity);

    [Function("Orders_Create")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")] HttpRequestData req)
    {

        var input = await HttpJson.ReadAsync<OrderCreate>(req);

        if (input == null || string.IsNullOrWhiteSpace(input.CustomerId) || string.IsNullOrWhiteSpace(input.ProductId) || input.Quantity < 1) 
            return HttpJson.Bad(req, "CustomerId, ProductId, Quantity >= 1 required");

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
        catch {
            return HttpJson.Bad(req, "Invaild CustomerId");
        }

        try
        {
            customer = (await customers.GetEntityAsync<CustomerEntity>("Customer", input.CustomerId)).Value;
        }
        catch
        {
            return HttpJson.Bad(req, "Invaild CustomerId");
        }

        if (product.StockAvailable < input.Quantity)
            return HttpJson.Bad(req, $"Insufficient stock. Available: {product.StockAvailable}");

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
        var queueOrder = new QueueClient(_conn, _queueOrder, new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
        var queueStock = new QueueClient(_conn, _queueStock, new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });

        await queueOrder.CreateIfNotExistsAsync();
        await queueStock.CreateIfNotExistsAsync();

        var orderMsg = new
        {
            Type = "OrderCreated",
            OrderId = order.RowKey,
            order.CustomerId,
            CustomerName = $"{customer.Name} {customer.Surname}",
            order.ProductId,
            ProductName = product.ProductName,
            order.Quantity,
            order.UnitPrice,
            TotalAmount = order.UnitPrice * order.Quantity,
            OrderDateUtc = order.OrderDateUtc,
            order.Status
        };
        await queueOrder.SendMessageAsync(JsonSerializer.Serialize(orderMsg));

        var stockMsg = new
        {
            Type = "StockUpdated",
            productId = product.RowKey,
            ProductName = product.ProductName,
            PreviousStock = product.StockAvailable + input.Quantity,
            NewStock = product.StockAvailable,
            UpdatedDateUtc = DateTimeOffset.UtcNow,
            UpdatedBy = "Order System"
        };
        await queueStock.SendMessageAsync(JsonSerializer.Serialize(stockMsg));

        return HttpJson.Created(req, Map.ToDto(order));
    }

    [Function("Orders_UpdateStatus")]
    public async Task<HttpResponseData> UpdateStatus(
    [HttpTrigger(AuthorizationLevel.Anonymous, "patch","post", "put", Route = "orders/{id}/status")] HttpRequestData req,
    string id)
    {

        var input = await HttpJson.ReadAsync<OrderStatusUpdate>(req);

        if (input == null || string.IsNullOrWhiteSpace(input.Status))
            return HttpJson.Bad(req, "Status is required");

        var orders = new TableClient(_conn, _ordersTable);

        try
        {
            var response = await orders.GetEntityAsync<OrderEntity>("Order", id);
            var entity = response.Value;
            var previous = entity.Status;

            entity.Status = input.Status;
            await orders.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);

            var queueOrder = new QueueClient(_conn, _queueOrder, new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
            await queueOrder.CreateIfNotExistsAsync();

            var statusMsg = new
            {

                Type = "OrderStatusUpdated",
                OrderId = entity.RowKey,
                PreviousStatus = previous,
                NewStatus = entity.Status,
                UpdatedDateUtc = DateTimeOffset.UtcNow,
                UpdatedBy = "System"
            };
            await queueOrder.SendMessageAsync(JsonSerializer.Serialize(statusMsg));

            return HttpJson.OK(req, Map.ToDto(entity));
        }
        catch
        { 
            return HttpJson.NotFound(req, "Order not found");
        }
    }

    [Function("Orders_Delete")]
    public async Task<HttpResponseData> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "orders/{id}")] HttpRequestData req, string id)
    {
        var table = new TableClient(_conn, _ordersTable);
        await table.DeleteEntityAsync("Order", id);
        return HttpJson.NoContent(req);
    }
}