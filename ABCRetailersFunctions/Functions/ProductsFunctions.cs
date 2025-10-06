using ABCRetailersFunctions.Entities;
using ABCRetailersFunctions.Helpers;
using ABCRetailersFunctions.Models;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using static Grpc.Core.Metadata;

namespace ABCRetailersFunctions.Functions;

public class ProductsFunctions
{
    private readonly string _conn;
    private readonly string _table;
    private readonly string _images;

    public ProductsFunctions(IConfiguration con)
    {
        _conn = con["STORAGE_CONNECTION"] ?? throw new InvalidOperationException("STORAGE_CONNECTION missing");
        _table = con["TABLE_PRODUCT"] ?? "Product";
        _images = con["BLOB_PRODUCT_IMAGES"] ?? "product-images";
    }

    [Function("Products_List")]
    public async Task<HttpResponseData> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products")] HttpRequestData req)
    {
        var table = new TableClient(_conn, _table);
        await table.CreateIfNotExistsAsync();

        var items = new List<ProductDto>();
        await foreach (var e in table.QueryAsync<ProductEntity>(x => x.PartitionKey == "Product"))
        {
            items.Add(Map.ToDto(e));
        }

        return HttpJson.OK(req, items);
    }

    [Function("Products_Get")]

    public async Task<HttpResponseData> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products/{id}")] HttpRequestData req,
        string id
        )
    {

        var table = new TableClient(_conn, _table);
        try
        {
            var entity = await table.GetEntityAsync<ProductEntity>("Product", id);
            return HttpJson.OK(req, Map.ToDto(entity.Value));
        }
        catch
        {
            return HttpJson.NotFound(req, "Product not Found");
        }
    }

    [Function("Product_Create")]

    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "products")] HttpRequestData req, FunctionContext ctx
        )
    {

        var contentType = req.Headers.TryGetValues("Content-Type", out var ct) ? ct.First() : "";
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

            var file = form.Files.FirstOrDefault(f => f.FileName == "ImageFile");
            if (file != null && file.Data.Length > 0)
            {
                var container = new BlobContainerClient(_conn, _images);
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
            price = body.TryGetValue("Price", out var pr) ? Convert.ToDouble(pr) : 0;
            stock = body.TryGetValue("StockAvailable", out var st) ? Convert.ToInt32(st) : 0;
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

    [Function("Products_Update")]
    public async Task<HttpResponseData> Update(
    [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "products/{id}")] HttpRequestData req,
    string id)
    {
        var contentType = req.Headers.TryGetValues("Content-Type", out var ct) ? ct.First() : "";
        var table = new TableClient(_conn, _table);

        try
        {
            var response = await table.GetEntityAsync<ProductEntity>("Product", id);
            var entity = response.Value;

            if (contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                var form = await MultipartHelper.ParseAsync(req.Body, contentType);

                if (form.Text.TryGetValue("ProductName", out var name)) entity.ProductName = name;
                if (form.Text.TryGetValue("Description", out var desc)) entity.Description = desc;
                if (form.Text.TryGetValue("Price", out var pr) && double.TryParse(pr, out var price)) entity.Price = price;
                if (form.Text.TryGetValue("StockAvailable", out var st) && int.TryParse(st, out var stock)) entity.StockAvailable = stock;
                if (form.Text.TryGetValue("ImageUrl", out var imageUrl)) entity.ImageUrl = imageUrl;

                var file = form.Files.FirstOrDefault(f => f.FieldName == "ImageFile");
                if (file != null && file.Data.Length > 0)
                {

                    var container = new BlobContainerClient(_conn, _images);
                    await container.CreateIfNotExistsAsync();
                    var blob = container.GetBlobClient($"{Guid.NewGuid():N}-{file.FileName}");
                    await using var s = file.Data;
                    await blob.UploadAsync(s, overwrite: false);
                    entity.ImageUrl = blob.Uri.ToString();
                }
            }
            else
            {
                var body = await HttpJson.ReadAsync<Dictionary<string, object>>(req) ?? new();
                if (body.TryGetValue("ProductName", out var pn)) entity.ProductName = pn?.ToString() ?? entity.ProductName;
                if (body.TryGetValue("Description", out var d)) entity.Description = d?.ToString() ?? entity.Description;
                if (body.TryGetValue("Price", out var pr) && double.TryParse(pr?.ToString(), out var price)) entity.Price = price;
                if (body.TryGetValue("StockAvailable", out var st) && int.TryParse(st?.ToString(), out var stock)) entity.StockAvailable = stock;
                if (body.TryGetValue("ImageUrl", out var iu)) entity.ImageUrl = iu?.ToString() ?? entity.ImageUrl;

            }

            await table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);

            return HttpJson.OK(req, Map.ToDto(entity));
        }
        catch
        {
            return HttpJson.NotFound(req, "Product not found");
        }
    }

    [Function("Products_Delete")]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "products/{id}")] HttpRequestData req, string id)
    {
        var table = new TableClient(_conn, _table);
        await table.DeleteEntityAsync("Product", id);
        return HttpJson.NoContent(req);
    }
}