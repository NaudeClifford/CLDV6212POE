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

public class CustomersFunctions
{
    private readonly string _conn;
    private readonly string _table;

    public CustomersFunctions(IConfiguration con)
    {
        _conn = con["STORAGE_CONNECTION"] ?? throw new InvalidOperationException();
        _table = con["TABLE_PRODUCT"] ?? "Customer";
    }

    [Function("Customer_List")]
    public async Task<HttpResponseData> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers")] HttpRequestData req)
    {
        var table = new TableClient(_conn, _table);
        await table.CreateIfNotExistsAsync();

        var items = new List<CustomerDto>();

        await foreach (var e in table.QueryAsync<CustomerEntity>(x => x.PartitionKey == "Customer"))
        {
            items.Add(Map.ToDto(e));
        }


        return HttpJson.OK(req, items);
    }

    [Function("Products_Get")]

    public async Task<HttpResponseData> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products/{id}")] HttpRequestData req
        , string id)
    {

        var table = new TableClient(_conn, _table);
        
        try
        {
            var e = await table.GetEntityAsync<ProductEntity>("Product", id);
            return HttpJson.OK(req, Map.ToDto(e.Value));
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return HttpJson.NotFound(req, "Product not found");
        }

    }

    [Function("Product_Create")]

    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "products")] HttpRequestData req
        )
    {

        var contentType = req.Headers.TryGetValues("Content-Type", out var ct) ? ct.ToString() : null;
        var table = new TableClient(_conn, _table);
        await table.CreateIfNotExistsAsync();

        string name = "", desc = "", imageUrl = "";
        double price = 0;
        int stock = 0;

        if (contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
        {
            var form = await MultipartHelper.ParseAsync(req.Body, contentType);
            
            name = form.Text.GetValueOrDefault("ProductName") ?? "";
            desc = form.Text.GetValueOrDefault("Description") ?? "";
            double.TryParse(form.Text.GetValueOrDefault("Price") ?? "0", out price);
            int.TryParse(form.Text.GetValueOrDefault("StockAvailable") ?? "0", out stock);

            var file = form.Files.FirstOrDefault(form => form.FileName == "ImageFile");
            if (file is not null && file.Data.Length > 0)
            {
                var container = new BlobContainerClient(_conn, _table);
                await container.CreateIfNotExistsAsync();
                var blob = container.GetBlobClient($"{Guid.NewGuid():N}-{file.FileName}");
                await using var s = file.Data;
                await blob.UploadAsync(s);
                imageUrl = blob.Uri.ToString();
            }
            else
            {
                imageUrl = form.Text.GetValueOrDefault("ImageUrl") ?? "";
            }
        }
        else
        {
            //JSON fallback
            var body = await HttpJson.ReadAsync<Dictionary<string, object>>(req) ?? new Dictionary<string, object>();
            name = body.TryGetValue("ProductName", out var pn) ? pn?.ToString() ?? "" : "";
            desc = body.TryGetValue("Description", out var d) ? d?.ToString() ?? "" : "";
            price = body.TryGetValue("Price", out var pr) && double.TryParse(pr?.ToString(), out var parsedPrice) ? parsedPrice : 0;
            stock = body.TryGetValue("StockAvailable", out var st) && int.TryParse(st?.ToString(), out var parsedStock) ? parsedStock : 0;
            imageUrl = body.TryGetValue("ImageUrl", out var iu) ? iu?.ToString() ?? "" : "";

        }

        if (string.IsNullOrWhiteSpace(name))
            return HttpJson.Bad(req, "ProductName is required");

        var e = new ProductEntity
        {
            ProductName = name,
            Description = desc,
            Price = price,
            StockAvailable = stock,
            ImageUrl = imageUrl
        };
        await table.AddEntityAsync(e);

        return HttpJson.Created(req, Map.ToDto(e));

    }

    [Function("Products_Update")]
    public async Task<HttpResponseData> Update(
    [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "products/{id}")] HttpRequestData req,
        string id)
    {
        var contentType = req.Headers.TryGetValues("Content-Type", out var ct) ? ct.FirstOrDefault() : null;
        
        var table = new TableClient(_conn, _table);

        try
        {
            var response = await table.GetEntityAsync<ProductEntity>("Product", id);
            
            var entity = response.Value;

            string name = entity.ProductName;
            
            string desc = entity.Description;
            
            double price = entity.Price;
            
            int stock = entity.StockAvailable;
            
            string imageUrl = entity.ImageUrl;

            if (contentType != null && contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                var form = await MultipartHelper.ParseAsync(req.Body, contentType);
                
                name = form.Text.GetValueOrDefault("ProductName") ?? name;
                
                desc = form.Text.GetValueOrDefault("Description") ?? desc;
                
                double.TryParse(form.Text.GetValueOrDefault("Price") ?? price.ToString(), out price);
                
                int.TryParse(form.Text.GetValueOrDefault("StockAvailable") ?? stock.ToString(), out stock);

                var file = form.Files.FirstOrDefault(f => f.FileName == "ImageFile");
                
                if (file is not null && file.Data.Length > 0)
                {
                    var container = new BlobContainerClient(_conn, _table);
                    await container.CreateIfNotExistsAsync();
                    var blob = container.GetBlobClient($"{Guid.NewGuid():N}-{file.FileName}");
                    await using var s = file.Data;
                    await blob.UploadAsync(s, overwrite: true);
                    imageUrl = blob.Uri.ToString();
                }
                else
                {
                    imageUrl = form.Text.GetValueOrDefault("ImageUrl") ?? imageUrl;
                }
            }
            else
            {
                var body = await HttpJson.ReadAsync<Dictionary<string, object>>(req);
                if (body is not null)
                {
                    name = body.TryGetValue("ProductName", out var pn) ? pn?.ToString() ?? name : name;
                    desc = body.TryGetValue("Description", out var d) ? d?.ToString() ?? desc : desc;
                    price = body.TryGetValue("Price", out var pr) && double.TryParse(pr?.ToString(), out var newPrice) ? newPrice : price;
                    stock = body.TryGetValue("StockAvailable", out var st) && int.TryParse(st?.ToString(), out var newStock) ? newStock : stock;
                    imageUrl = body.TryGetValue("ImageUrl", out var iu) ? iu?.ToString() ?? imageUrl : imageUrl;
                }
            }

            entity.ProductName = name;
            entity.Description = desc;
            entity.Price = price;
            entity.StockAvailable = stock;
            entity.ImageUrl = imageUrl;

            await table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);

            return HttpJson.OK(req, Map.ToDto(entity));
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return HttpJson.NotFound(req, "Product not found");
        }
        catch (Exception ex)
        {
            return HttpJson.Bad(req, $"Internal error: {ex.Message}");
        }
    }

}