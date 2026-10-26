using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Brouj.API.Controllers;
using Brouj.Application.Common.Models;
using Microsoft.AspNetCore.Http;

namespace Brouj.API.IntegrationTests.Support;

internal static class HttpTestRequests
{
    public static IReadOnlyList<EndpointCase> Contracts { get; } = LoadContracts();

    public static Type Resolve(string name) =>
        typeof(ApiControllerBase).Assembly.GetTypes().Concat(typeof(Result).Assembly.GetTypes())
            .Single(type => type.Name == name);

    public static Guid RouteId(string parameter) => parameter switch
    {
        "imageId" => Guid.Parse("22222222-2222-2222-2222-222222222222"),
        "stageId" or "timePlanId" or "itemId" => Guid.Parse("33333333-3333-3333-3333-333333333333"),
        _ => Guid.Parse("11111111-1111-1111-1111-111111111111")
    };

    public static HttpRequestMessage Create(EndpointCase endpoint)
    {
        var path = Regex.Replace(endpoint.Route, "\\{(\\w+)\\}", match => RouteId(match.Groups[1].Value).ToString());

        if (endpoint.Query is not null)
        {
            var fields = Resolve(endpoint.Query).GetProperties().Select(property =>
                Uri.EscapeDataString(property.Name) + "=" + Uri.EscapeDataString(QueryValue(property)));
            path += "?" + string.Join("&", fields);
        }

        var request = new HttpRequestMessage(new HttpMethod(endpoint.Method), path);

        if (endpoint.Body is null)
        {
            return request;
        }

        if (endpoint.RateLimit == "FileUploads")
        {
            request.Content = ImageContent();
        }
        else
        {
            var type = Resolve(endpoint.Body);
            request.Content = JsonContent.Create(Sample(type), type);
        }

        return request;
    }

    public static MultipartFormDataContent ImageContent(int size = 16, string name = "image.png", bool signature = true)
    {
        var data = new byte[size];

        if (signature && size >= 8)
        {
            new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }.CopyTo(data, 0);
        }

        var file = new ByteArrayContent(data);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var content = new MultipartFormDataContent();
        content.Add(file, "File", name);
        content.Add(new StringContent("Image description"), "AltText");
        content.Add(new StringContent("Image description"), "Description");

        return content;
    }

    public static async Task AddProofAsync(HttpClient client, HttpRequestMessage request, string credential)
    {
        using var bootstrap = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/csrf");

        if (credential.Length > 0)
        {
            bootstrap.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential);
        }

        using var response = await client.SendAsync(bootstrap);
        response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var token = json.RootElement.GetProperty("data").GetProperty("requestToken").GetString();
        var cookie = response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith("__Host-brouj-csrf=", StringComparison.Ordinal)).Split(';')[0];
        request.Headers.Add("Cookie", cookie);
        request.Headers.Add("X-CSRF-TOKEN", token);
    }

    private static string QueryValue(PropertyInfo property)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (type == typeof(string))
        {
            return "example";
        }

        if (type == typeof(Guid))
        {
            return RouteId(property.Name).ToString();
        }

        if (type == typeof(DateTimeOffset))
        {
            return "2026-01-01T00:00:00Z";
        }

        return type == typeof(bool) ? "true" : type.IsEnum ? Enum.GetValues(type).GetValue(0)!.ToString()! : "2";
    }

    private static object? Sample(Type type)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;

        if (actual == typeof(string))
        {
            return "Example";
        }

        if (actual == typeof(Guid))
        {
            return Guid.NewGuid();
        }

        if (actual == typeof(DateTimeOffset))
        {
            return DateTimeOffset.UtcNow;
        }

        if (actual.IsGenericType && actual.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
        {
            return Array.CreateInstance(actual.GenericTypeArguments[0], 0);
        }

        var value = Activator.CreateInstance(actual);

        if (actual.IsPrimitive || actual.IsEnum || actual == typeof(decimal))
        {
            return value;
        }

        foreach (var property in actual.GetProperties().Where(property => property.CanWrite))
        {
            property.SetValue(value, Sample(property.PropertyType));
        }

        return value;
    }

    private static IReadOnlyList<EndpointCase> LoadContracts()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Brouj.slnx")))
        {
            directory = directory.Parent;
        }

        var path = Path.Combine(directory!.FullName, "docs", "Brouj.Docs", "Part8.EndpointContracts.json");

        return JsonSerializer.Deserialize<EndpointCase[]>(File.ReadAllText(path), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }
}
