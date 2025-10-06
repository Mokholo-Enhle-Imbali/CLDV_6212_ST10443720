using ABCRetailersFunction.Entities;
using ABCRetailersFunction.Helpers;
using ABCRetailersFunction.Models;
using Azure.Data.Tables;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace ABCRetailersFunction.Functions;

public class CustomerFunctions
{
    private readonly string _conn;
    private readonly string _table;

    public CustomerFunctions(IConfiguration cfg)
    {
        _conn = cfg["STORAGE_CONNECTION"] ?? throw new InvalidOperationException("STORAGE_CONNECTION missing");
        _table = cfg["TABLE_CUSTOMER"] ?? "Customer";
    }

    [Function("CustomerList")]
    public async Task<HttpResponseData> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customer")] HttpRequestData req)
    {
        var table = new TableClient(_conn, _table);
        await table.CreateIfNotExistsAsync();

        var items = new List<CustomerDto>();
        await foreach (var e in table.QueryAsync<CustomerEntity>(x => x.PartitionKey == "Customer"))
            items.Add(Map.ToDto(e));

        return await HttpJson.OkAsync(req, items);
    }

    [Function("CustomerGet")]
    public async Task<HttpResponseData> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customer/{id}")] HttpRequestData req, string id)
    {
        var table = new TableClient(_conn, _table);
        try
        {
            var e = await table.GetEntityAsync<CustomerEntity>("Customer", id);
            return await HttpJson.OkAsync(req, Map.ToDto(e.Value));
        }
        catch
        {
            return await HttpJson.OkAsync(req, "Customer not found");
        }
    }

    public record CustomerCreateUpdate(string? Name, string? Surname, string? Username, string? Email, string? ShippingAddress);

    [Function("CustomerCreate")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customer")] HttpRequestData req)
    {
        var input = await HttpJson.ReadAsync<CustomerCreateUpdate>(req);
        if (input is null || string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.Email))
            return await HttpJson.OkAsync(req, "Name and Email are required");

        var table = new TableClient(_conn, _table);
        await table.CreateIfNotExistsAsync();

        var e = new CustomerEntity
        {
            Name = input.Name!,
            Surname = input.Surname ?? "",
            Username = input.Username ?? "",
            Email = input.Email!,
            ShippingAddress = input.ShippingAddress ?? ""
        };
        await table.AddEntityAsync(e);

        return await HttpJson.OkAsync(req, Map.ToDto(e));
    }

    [Function("CustomerUpdate")]
    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "customer/{id}")] HttpRequestData req, string id)
    {
        var input = await HttpJson.ReadAsync<CustomerCreateUpdate>(req);
        if (input is null) return await HttpJson.OkAsync(req, "Invalid body");

        var table = new TableClient(_conn, _table);
        try
        {
            var resp = await table.GetEntityAsync<CustomerEntity>("Customer", id);
            var e = resp.Value;

            e.Name = input.Name ?? e.Name;
            e.Surname = input.Surname ?? e.Surname;
            e.Username = input.Username ?? e.Username;
            e.Email = input.Email ?? e.Email;
            e.ShippingAddress = input.ShippingAddress ?? e.ShippingAddress;

            await table.UpdateEntityAsync(e, e.ETag, TableUpdateMode.Replace);
            return await HttpJson.OkAsync(req, Map.ToDto(e));
        }
        catch
        {
            return await HttpJson.OkAsync(req, "Customer not found");
        }
    }

    [Function("Customers_Delete")]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "customer/{id}")] HttpRequestData req, string id)
    {
        var table = new TableClient(_conn, _table);
        await table.DeleteEntityAsync("Customer", id);
        return HttpJson.NoContent(req);
    }
}