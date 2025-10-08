using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ABCRetailersFunctions.Helpers;

public static class HttpJson
{
    public static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public static async Task<T> ReadAsync<T>(HttpRequestData req)
    {
        using var s = req.Body;
        var result = await JsonSerializer.DeserializeAsync<T>(s, _json);

        if (result == null)
            throw new InvalidDataException("Request body could not be deserialized.");

        return result;
    }

    public static Task<HttpResponseData> OK<T>(HttpRequestData req, T body)
        => WriteAsync(req, HttpStatusCode.OK, body);

    public static Task<HttpResponseData> Created<T>(HttpRequestData req, T body)
        => WriteAsync(req, HttpStatusCode.Created, body);

    public static Task<HttpResponseData> Bad(HttpRequestData req, string message)
        => TextAsync(req, HttpStatusCode.BadRequest, message);

    public static Task<HttpResponseData> NoContent(HttpRequestData req)
    {
        var r = req.CreateResponse(HttpStatusCode.NoContent);
        return Task.FromResult(r);
    }

    public static Task<HttpResponseData> NotFound(HttpRequestData req, string message = "Not Found")
        => TextAsync(req, HttpStatusCode.NotFound, message);

    public static async Task<HttpResponseData> TextAsync(HttpRequestData req, HttpStatusCode code, string message)
    {
        var r = req.CreateResponse(code);
        r.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        await r.WriteStringAsync(message, Encoding.UTF8);
        return r;
    }


    public static async Task<HttpResponseData> WriteAsync<T>(HttpRequestData req, HttpStatusCode code, T body)
    {
        if (req == null) throw new ArgumentNullException(nameof(req));

        var response = req.CreateResponse(code);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");

        try
        {
            await JsonSerializer.SerializeAsync(response.Body, body, _json);
        }
        catch (JsonException)
        {
            response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("Error serializing response", Encoding.UTF8);
        }
        return response;
    }
}
