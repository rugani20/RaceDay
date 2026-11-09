using System.Net;
using System.Text;
using System.Text.Json;
namespace RaceDay.Mvc.Services;

public class ApiResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Error { get; set; } = "";
}

/// <summary>Single gateway to the Part 2 REST API. Adjust the route strings in the controllers if your API differs.</summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _ctx;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public ApiClient(HttpClient http, IHttpContextAccessor ctx) { _http = http; _ctx = ctx; }

    public Task<ApiResult<T>> GetAsync<T>(string url) => Send<T>(new HttpRequestMessage(HttpMethod.Get, url));
    public Task<ApiResult<T>> PostAsync<T>(string url, object? body) => Send<T>(Build(HttpMethod.Post, url, body));
    public Task<ApiResult<T>> PutAsync<T>(string url, object? body) => Send<T>(Build(HttpMethod.Put, url, body));
    public Task<ApiResult<object>> DeleteAsync(string url) => Send<object>(new HttpRequestMessage(HttpMethod.Delete, url));
    public Task<ApiResult<T>> SendFormAsync<T>(HttpMethod method, string url, Dictionary<string, string> fields, IFormFile? file, string fileField)
    {
        var form = new MultipartFormDataContent();
        foreach (var f in fields) form.Add(new StringContent(f.Value ?? ""), f.Key);
        if (file is { Length: > 0 })
        {
            var sc = new StreamContent(file.OpenReadStream());
            sc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            form.Add(sc, fileField, file.FileName);
        }
        return Send<T>(new HttpRequestMessage(method, url) { Content = form });
    }

    private static HttpRequestMessage Build(HttpMethod m, string url, object? body) =>
        new(m, url) { Content = body == null ? null : new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json") };

    private async Task<ApiResult<T>> Send<T>(HttpRequestMessage req)
    {
        try
        {
            var cookie = _ctx.HttpContext?.Session.GetString("ApiCookie");

            if (!string.IsNullOrEmpty(cookie)) req.Headers.TryAddWithoutValidation("Cookie", cookie);

            var res = await _http.SendAsync(req);

            if (res.Headers.TryGetValues("Set-Cookie", out var setCookies) && _ctx.HttpContext != null)

            {

                var jar = new Dictionary<string, string>();

                foreach (var p in (_ctx.HttpContext.Session.GetString("ApiCookie") ?? "").Split("; ", StringSplitOptions.RemoveEmptyEntries))

                { var kv = p.Split('=', 2); if (kv.Length == 2) jar[kv[0]] = kv[1]; }

                foreach (var c in setCookies)

                { var kv = c.Split(';')[0].Split('=', 2); if (kv.Length == 2) jar[kv[0]] = kv[1]; }

                _ctx.HttpContext.Session.SetString("ApiCookie", string.Join("; ", jar.Select(k => $"{k.Key}={k.Value}")));

            }


       
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode) return new() { Error = Friendly(res.StatusCode, body) };
            T? data = default;
            if (!string.IsNullOrWhiteSpace(body) && typeof(T) != typeof(object))
                data = JsonSerializer.Deserialize<T>(body, Json);
            return new() { Success = true, Data = data };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("API ERROR: " + ex);
            // Never leak raw exceptions to the user
            
            return new() { Error = "We could not reach the RaceDay service. Please try again shortly." };
        }
    }

    private static string Friendly(HttpStatusCode code, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            foreach (var key in new[] { "message", "detail", "title" })
                if (doc.RootElement.TryGetProperty(key, out var p) && p.ValueKind == JsonValueKind.String && p.GetString()!.Length < 160)
                    return p.GetString()!;
        }
        catch { /* not JSON */ }
        return code switch
        {
            HttpStatusCode.BadRequest => "Some of the details you entered are invalid. Please check and try again.",
            HttpStatusCode.Unauthorized => "Incorrect credentials or your session has expired.",
            HttpStatusCode.Forbidden => "You do not have permission to do that.",
            HttpStatusCode.NotFound => "We could not find what you were looking for.",
            HttpStatusCode.Conflict => "That already exists (for example, you may already be enrolled).",
            _ => "Something went wrong. Please try again."
        };
    }
}
