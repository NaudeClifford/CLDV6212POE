using ABCRetailersFunctions.Entities;
using ABCRetailersFunctions.Helpers;
using ABCRetailersFunctions.Models;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;

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
        _conn = con["STORAGE_CONNECTION"] ?? throw new InvalidOperationException();
        _ordersTable = con["TABLE_ORDER"] ?? "Order";
        _productsTable = con["TABLE_PRODUCT"] ?? "Product";
        _customersTable = con["TABLE_CUSTOMER"] ?? "Customer";
        _queueOrder = con["QUEUE_ORDER_NOTIFICATIONS"] ?? "order_notifications";
        _queueStock = con["QUEUE_STOCK_UPDATES"] ?? "stock_updates";
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

        var ordered = items.OrderByDescending(o => o.OrderDate).ToList();
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
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return HttpJson.NotFound(req, "Order not found");
        }
    }

    [Function("Orders_Create")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")] HttpRequestData req)
    {
        var contentType = req.Headers.TryGetValues("Content-Type", out var contentTypes)
            ? contentTypes.FirstOrDefault()
            : null;

        var table = new TableClient(_conn, _ordersTable);
        await table.CreateIfNotExistsAsync();

        string name = "", desc = "", imageUrl = "";
        double price = 0;
        int stock = 0;

        if (!string.IsNullOrWhiteSpace(contentType) &&
            contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
        {
            var form = await MultipartHelper.ParseAsync(req.Body, contentType);
            name = form.Text.GetValueOrDefault("ProductName") ?? "";
            desc = form.Text.GetValueOrDefault("Description") ?? "";
            double.TryParse(form.Text.GetValueOrDefault("Price"), out price);
            int.TryParse(form.Text.GetValueOrDefault("StockAvailable"), out stock);

            var file = form.Files.FirstOrDefault(f => f.FileName == "ImageFile");
            if (file != null && file.Data.Length > 0)
            {
                var container = new BlobContainerClient(_conn, _productsTable.ToLower());
                await container.CreateIfNotExistsAsync();
                var blob = container.GetBlobClient($"{Guid.NewGuid():N}-{file.FileName}");
                await using var stream = file.Data;
                await blob.UploadAsync(stream);
                imageUrl = blob.Uri.ToString();
            }
            else
            {
                imageUrl = form.Text.GetValueOrDefault("ImageUrl") ?? "";
            }
        }
        else
        {
            var body = await HttpJson.ReadAsync<Dictionary<string, object>>(req) ?? new();
            name = body.TryGetValue("ProductName", out var pn) ? pn?.ToString() ?? "" : "";
            desc = body.TryGetValue("Description", out var d) ? d?.ToString() ?? "" : "";
            price = body.TryGetValue("Price", out var pr) && double.TryParse(pr?.ToString(), out var p) ? p : 0;
            stock = body.TryGetValue("StockAvailable", out var st) && int.TryParse(st?.ToString(), out var s) ? s : 0;
            imageUrl = body.TryGetValue("ImageUrl", out var iu) ? iu?.ToString() ?? "" : "";
        }

        if (string.IsNullOrWhiteSpace(name))
            return HttpJson.Bad(req, "ProductName is required");

        var entity = new ProductEntity
        {
            ProductName = name,
            Description = desc,
            Price = price,
            StockAvailable = stock,
            ImageUrl = imageUrl
        };

        await table.AddEntityAsync(entity);
        return HttpJson.Created(req, Map.ToDto(entity));
    }

    [Function("Orders_Update")]
    public async Task<HttpResponseData> Update(
    [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "orders/{id}")] HttpRequestData req,
    string id)
    {
        var table = new TableClient(_conn, _ordersTable);

        try
        {
            var response = await table.GetEntityAsync<OrderEntity>("Order", id);
            var entity = response.Value;

            var body = await HttpJson.ReadAsync<Dictionary<string, object>>(req);
            if (body == null)
                return HttpJson.Bad(req, "Request body is empty");

            // Update only valid order fields
            if (body.TryGetValue("Quantity", out var q) && int.TryParse(q?.ToString(), out var quantity))
                entity.Quantity = quantity;

            if (body.TryGetValue("UnitPrice", out var p) && double.TryParse(p?.ToString(), out var price))
                entity.UnitPrice = price;

            if (body.TryGetValue("Status", out var s))
                entity.Status = s?.ToString() ?? entity.Status;

            entity.TotalPrice = entity.Quantity * entity.UnitPrice;

            await table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);

            return HttpJson.OK(req, Map.ToDto(entity));
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return HttpJson.NotFound(req, "Order not found");
        }
        catch (Exception ex)
        {
            return HttpJson.Bad(req, $"Internal error: {ex.Message}");
        }
    }
}