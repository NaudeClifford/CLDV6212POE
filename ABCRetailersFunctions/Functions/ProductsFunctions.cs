using ABCRetailersFunctions.Entities;
using ABCRetailersFunctions.Helpers;
using ABCRetailersFunctions.Models;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;


namespace ABCRetailersFunctions.Functions;

public class ProductsFunctions
{
    private readonly string _conn;
    private readonly string _table;
    private readonly string _images;

    public ProductsFunctions(IConfiguration con)
    {
        _conn = con["STORAGE_CONNECTION"] ?? throw new InvalidOperationException("Missing STORAGE_CONNECTION");
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

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(items);
        return response;
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
            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteAsJsonAsync(Map.ToDto(entity.Value));
            return response;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            var response = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
            await response.WriteStringAsync("Product not found");
            return response;
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

        if (!string.IsNullOrWhiteSpace(contentType) && contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
        {
            var form = await MultipartHelper.ParseAsync(req.Body, contentType);
            name = form.Text.GetValueOrDefault("ProductName") ?? "";
            desc = form.Text.GetValueOrDefault("Description") ?? "";
            double.TryParse(form.Text.GetValueOrDefault("Price"), out price);
            int.TryParse(form.Text.GetValueOrDefault("StockAvailable"), out stock);

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
            price = body.TryGetValue("Price", out var pr) && double.TryParse(pr?.ToString(), out var p) ? p : 0;
            stock = body.TryGetValue("StockAvailable", out var st) && int.TryParse(st?.ToString(), out var s) ? s : 0;
            imageUrl = body.TryGetValue("ImageUrl", out var iu) ? iu?.ToString() ?? "" : "";
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("ProductName is required");
            return badResponse;
        }

        var entity = new ProductEntity
        {
            PartitionKey = "Product",
            RowKey = Guid.NewGuid().ToString(),
            ProductName = name,
            Description = desc,
            Price = price,
            StockAvailable = stock,
            ImageUrl = imageUrl
        };

        await table.AddEntityAsync(entity);

        var createdResponse = req.CreateResponse(System.Net.HttpStatusCode.Created);
        await createdResponse.WriteAsJsonAsync(Map.ToDto(entity));
        return createdResponse;

    }

    [Function("Products_Update")]
    public async Task<HttpResponseData> Update(
    [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "products/{id}")] HttpRequestData req,
    string id)
    {
        var contentType = req.Headers.TryGetValues("Content-Type", out var ctValues) ? ctValues.FirstOrDefault() : null;
        var table = new TableClient(_conn, _table);

        try
        {
            var response = await table.GetEntityAsync<ProductEntity>("Product", id);
            var entity = response.Value;

            if (!string.IsNullOrWhiteSpace(contentType) && contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                var form = await MultipartHelper.ParseAsync(req.Body, contentType);

                if (form.Text.TryGetValue("ProductName", out var name)) entity.ProductName = name;
                if (form.Text.TryGetValue("Description", out var desc)) entity.Description = desc;
                if (form.Text.TryGetValue("Price", out var pr) && double.TryParse(pr, out var price)) entity.Price = price;
                if (form.Text.TryGetValue("StockAvailable", out var st) && int.TryParse(st, out var stock)) entity.StockAvailable = stock;
                if (form.Text.TryGetValue("ImageUrl", out var imageUrl)) entity.ImageUrl = imageUrl;
            }
            else
            {
                var body = await HttpJson.ReadAsync<Dictionary<string, object>>(req);
                if (body is not null)
                {
                    if (body.TryGetValue("ProductName", out var pn)) entity.ProductName = pn?.ToString() ?? entity.ProductName;
                    if (body.TryGetValue("Description", out var d)) entity.Description = d?.ToString() ?? entity.Description;
                    if (body.TryGetValue("Price", out var pr) && double.TryParse(pr?.ToString(), out var price)) entity.Price = price;
                    if (body.TryGetValue("StockAvailable", out var st) && int.TryParse(st?.ToString(), out var stock)) entity.StockAvailable = stock;
                    if (body.TryGetValue("ImageUrl", out var iu)) entity.ImageUrl = iu?.ToString() ?? entity.ImageUrl;
                }
            }

            await table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);

            var okResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await okResponse.WriteAsJsonAsync(Map.ToDto(entity));
            return okResponse;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
            await notFoundResponse.WriteStringAsync("Product not found");
            return notFoundResponse;
        }
    }
}