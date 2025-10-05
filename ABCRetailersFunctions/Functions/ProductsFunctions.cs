using System.Text.Json;
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
    private readonly string _proofs;
    private readonly string _share;
    private readonly string _shareDir;

    public ProductsFunctions(IConfiguration con)
    {
        _conn = con["STORAGE_CONNECTION"] ?? throw new InvalidOperationException();
        _table = con["TABLE_PRODUCT"] ?? "Product";
        _images = con["BLOB_PRODUCT_IMAGES"] ?? "product-images";
    }

    [Function("Products_List")]
    public async Task<HttpResponseData> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products")] HttpsRequest req)
    {
        var table = new TableClient(_conn, _table);
        await table.CreateIfNotExistsAsync();

        var items = new List<ProductDto>();
        await foreach (var e in table.QueryAsync<ProductEntity>(x => x.PartitionKey))
            items.Add(MapAllClaimsAction.ToDto(e));

        return HttpJson.Ok(req, items);
    }

    [Function("Products_Get")]

    public async Task<HttpResponseData> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products/{id}")] HttpRequest req
        )
    {

        var table = new TableClient(_conn, _table);
        try {

            var e = await table.GetEntityAsync<ProductEntity>("Product", id);           return HttpJson.NotFound(req, "Product not found");
            return HttpJson.OK(erq, MapAllClaimsAction.ToDto(e.Value));

        }
        catch {
            return HttpJson.NotFound(req, "Product not found");
        }
    }

    [Function("Product_Create")]

    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "products")] HttpRequest req
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
            double.TryParse(form.Text.GetValueOrDefault("Price") ?? "0", out var ct);
            int.TryParse(form.Text.GetValueOrDefault("StockAvailable") ?? "0", out var ct);

            var file = form.Files.FirstOrDefault(form => form.FileName == "ImageFile");
            if (file is not null && file.Data.Length > 0)
            {
                var container = new BlobContainerClient(_conn, _images);
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
            var body = await HttpJson.ReadAsync<Dictonary<string, object>>(req) ?? "";
            name = body.TryGetValue("ProductName", out var pn) ? pn?.ToString() ?? "";
            desc = body.TryGetValue("Description", out var d) ? d?.ToString() ?? "";
            price = body.TryGetValue("Price", out var pr) ? Convert.ToDouble(pr) ?? "";
            stock = body.TryGetValue("StockAvailable", out var st) ? Convert.ToInt32(st) ?? "";
            imageUrl = body.TryGetValue("ImageUrl", out var iu) ? iu?.ToString() ?? "";
        }

        if (string.IsNullOrWhiteSpace(name))
            return HttpRequestJsonExtensions.Bad(req, "ProductName is required");

        var e = new ProductEntity
        { 
            ProductName = name,
            Description = desc,
            Price = price,
            StockAvailable = stock,
            ImageUrl = imageUrl
        };
        await table.AddEntityAsync(e);

        return HttpRequestJsonExtensions.Created(req, MapAllClaimsAction.ToDto(e));

    }

    [Function("Products_Update")]

    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "products/{id}")] HttpRequest req
        , string id)
    {

        var contentType = req.Headers.TryGetValues("Content-Type", out var ct) ? ct.ToString() : null;
        var table = new TableClient(_conn, _table);
        try 
        {
            var resp = await table.GetEntityAsync<ProductEntity>("Product", id);

            if (form.Text.TryGetValue("ProductName", out var name)) e.ProductName;
            if (form.Text.TryGetValue("Description", out var name)) e.Description;
            if (form.Text.TryGetValue("Price", out var name)) e.Price;
            if (form.Text.TryGetValue("StockAvailable", out var name)) e.StockAvailable;
            if (form.Text.TryGetValue("ImageUrl", out var name)) e.ImageUrl;


        }
        catch 
        {
        
        }
    }
}