using ABCRetailersFunctions.Entities;
using ABCRetailersFunctions.Helpers;
using ABCRetailersFunctions.Models;
using Azure.Data.Tables;
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
        _conn = con["STORAGE_CONNECTION"] ?? throw new InvalidOperationException("STORAGE_CONNECTION missing");
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

    [Function("Customers_Get")]
    public async Task<HttpResponseData> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers/{id}")] HttpRequestData req
        , string id)
    {

        var table = new TableClient(_conn, _table);
        
        try
        {
            var e = await table.GetEntityAsync<CustomerEntity>("Customer", id);
            return HttpJson.OK(req, Map.ToDto(e.Value));
        }
        catch
        {
            return HttpJson.NotFound(req, "Customer not found");
        }

    }

    public record CustomerCreateUpdate(string? Name, string? Surname, string? Username, string? Email, string? ShippingAddress);

    [Function("Customers_Create")]

    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers")] HttpRequestData req)
    {

        var input = await HttpJson.ReadAsync<CustomerCreateUpdate>(req);

        if (input == null || string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.Email))
            return HttpJson.Bad(req, "Name and Email are required");

        var table = new TableClient(_conn, _table);
        await table.CreateIfNotExistsAsync();

        var e = new CustomerEntity
        {
            Name = input.Name,
            Surname = input.Surname ?? "",
            Username = input.Username ?? "",
            Email = input.Email,
            ShippingAddress = input.ShippingAddress ?? ""
        };
        await table.AddEntityAsync(e);

        return HttpJson.Created(req, Map.ToDto(e));

    }

    [Function("Customer_Update")]
    public async Task<HttpResponseData> Update(
    [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "customers/{id}")] HttpRequestData req,
        string id)
    {
        var input = await HttpJson.ReadAsync<CustomerCreateUpdate>(req);

        if (input == null) return HttpJson.Bad(req, "Invalid body");

        var table = new TableClient(_conn, _table);

        try
        {
            var response = await table.GetEntityAsync<CustomerEntity>("Customer", id);

            var entity = response.Value;

            entity.Name = input.Name ?? entity.Name;

            entity.Surname = input.Surname ?? entity.Surname;

            entity.Username = input.Username ?? entity.Username;

            entity.Email = input.Email ?? entity.Email;

            entity.ShippingAddress = input.ShippingAddress ?? entity.ShippingAddress;

            await table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);

            return HttpJson.OK(req, Map.ToDto(entity));
        }
        catch (Exception ex)
        {
            return HttpJson.NotFound(req, "Customer not found");
        }
    }
}