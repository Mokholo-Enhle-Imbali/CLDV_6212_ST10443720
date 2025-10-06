using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ABCRetailersFunction.Helpers
{
    public static class HttpJson
    {
        static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public static async Task<T?> ReadAsync<T>(HttpRequestData req)
        {
            using var s = req.Body;
            return await JsonSerializer.DeserializeAsync<T>(s, _json);
        }

        public static async Task<HttpResponseData> OkAsync<T>(HttpRequestData req, T body)
            => await WriteAsync(req, HttpStatusCode.OK, body);

        public static async Task<HttpResponseData> CreatedAsync<T>(HttpRequestData req, T body)
            => await WriteAsync(req, HttpStatusCode.Created, body);

        public static async Task<HttpResponseData> BadAsync(HttpRequestData req, string message)
            => await TextAsync(req, HttpStatusCode.BadRequest, message);

        public static async Task<HttpResponseData> NotFoundAsync(HttpRequestData req, string message = "Not Found")
            => await TextAsync(req, HttpStatusCode.NotFound, message);

        public static HttpResponseData NoContent(HttpRequestData req)
        {
            var r = req.CreateResponse(HttpStatusCode.NoContent);
            return r;
        }

        public static async Task<HttpResponseData> TextAsync(HttpRequestData req, HttpStatusCode code, string message)
        {
            var r = req.CreateResponse(code);
            r.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            await r.WriteStringAsync(message, Encoding.UTF8);
            return r;
        }

        private static async Task<HttpResponseData> WriteAsync<T>(HttpRequestData req, HttpStatusCode code, T body)
        {
            var r = req.CreateResponse(code);
            r.Headers.Add("Content-Type", "application/json; charset=utf-8");
            var json = JsonSerializer.Serialize(body, _json);
            await r.WriteStringAsync(json, Encoding.UTF8);
            return r;
        }

        // Keep the old synchronous methods for backward compatibility if needed
        // but mark them as obsolete
        [Obsolete("Use OkAsync instead - synchronous operations are disallowed")]
        public static HttpResponseData Ok<T>(HttpRequestData req, T body)
            => Write(req, HttpStatusCode.OK, body);

        [Obsolete("Use CreatedAsync instead - synchronous operations are disallowed")]
        public static HttpResponseData Created<T>(HttpRequestData req, T body)
            => Write(req, HttpStatusCode.Created, body);

        [Obsolete("Use BadAsync instead - synchronous operations are disallowed")]
        public static HttpResponseData Bad(HttpRequestData req, string message)
            => Text(req, HttpStatusCode.BadRequest, message);

        [Obsolete("Use NotFoundAsync instead - synchronous operations are disallowed")]
        public static HttpResponseData NotFound(HttpRequestData req, string message = "Not Found")
            => Text(req, HttpStatusCode.NotFound, message);

        private static HttpResponseData Write<T>(HttpRequestData req, HttpStatusCode code, T body)
        {
            var r = req.CreateResponse(code);
            r.Headers.Add("Content-Type", "application/json; charset=utf-8");
            var json = JsonSerializer.Serialize(body, _json);
            r.WriteString(json, Encoding.UTF8); // This will throw the error
            return r;
        }

        private static HttpResponseData Text(HttpRequestData req, HttpStatusCode code, string message)
        {
            var r = req.CreateResponse(code);
            r.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            r.WriteString(message, Encoding.UTF8); // This will throw the error
            return r;
        }
    }
}