using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Brouj.API.IntegrationTests.Support;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.ContactSubmissions.DTOs;
using Brouj.Domain.Entities;

namespace Brouj.API.IntegrationTests;

public sealed class ContactSubmissionTests
{
    private const string SubmitRoute = "/api/v1/contact-submissions";
    private const string ListRoute = "/api/v1/admin/contact-submissions";
    private const string Phone = "01012345678";

    [Fact]
    public async Task Anonymous_submission_requires_csrf_and_stores_trimmed_unicode_and_multiline_text()
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        var body = new { name = "  أحمد علي  ", phone = $" {Phone} ", inquiry = "  السطر الأول\nالسطر الثاني  " };

        using var missingProof = await client.PostAsJsonAsync(SubmitRoute, body);
        Assert.Equal(HttpStatusCode.BadRequest, missingProof.StatusCode);
        Assert.Empty(factory.Database.ContactSubmissions);

        var ids = new List<Guid>();
        for (var index = 0; index < 2; index++)
        {
            using var response = await SubmitAsync(client, body);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
            ids.Add(payload!.Data);
        }

        Assert.Equal(2, ids.Distinct().Count());
        Assert.All(factory.Database.ContactSubmissions, submission =>
        {
            Assert.Contains(submission.Id, ids);
            Assert.Equal("أحمد علي", submission.Name);
            Assert.Equal(Phone, submission.Phone);
            Assert.Equal("السطر الأول\nالسطر الثاني", submission.Inquiry);
        });
        Assert.Equal(2, factory.Database.ContactSubmissions.Count());
    }

    [Theory]
    [InlineData(null, Phone, "Inquiry")]
    [InlineData(" ", Phone, "Inquiry")]
    [InlineData("Name", "", "Inquiry")]
    [InlineData("Name", "01312345678", "Inquiry")]
    [InlineData("Name", "+201012345678", "Inquiry")]
    [InlineData("Name", "٠١٠١٢٣٤٥٦٧٨", "Inquiry")]
    [InlineData("Name", Phone, "\r\n ")]
    public async Task Invalid_fields_return_400_without_saving(string? name, string phone, string inquiry)
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        using var response = await SubmitAsync(client, new { name, phone, inquiry });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.Database.ContactSubmissions);
    }

    [Theory]
    [InlineData(150, 2000, HttpStatusCode.Created)]
    [InlineData(151, 2000, HttpStatusCode.BadRequest)]
    [InlineData(150, 2001, HttpStatusCode.BadRequest)]
    public async Task Text_lengths_are_checked_after_trimming(int nameLength, int inquiryLength, HttpStatusCode expected)
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        using var response = await SubmitAsync(client, new
        {
            name = $" {new string('ن', nameLength)} ",
            phone = Phone,
            inquiry = $" {new string('س', inquiryLength)} "
        });

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(expected == HttpStatusCode.Created ? 1 : 0, factory.Database.ContactSubmissions.Count());
    }

    [Fact]
    public async Task Default_submission_limit_rejects_the_sixth_request()
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();

        for (var index = 0; index < 6; index++)
        {
            using var response = await SubmitAsync(client, new { name = "Name", phone = Phone, inquiry = "Inquiry" });
            Assert.Equal(index < 5 ? HttpStatusCode.Created : HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        Assert.Equal(5, factory.Database.ContactSubmissions.Count());
    }

    [Theory]
    [InlineData("AdminOnly")]
    [InlineData("SuperAdminOnly")]
    public async Task Staff_list_is_paginated_and_orders_matching_timestamps_by_id(string role)
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Credential(role));
        var time = new DateTimeOffset(2026, 11, 20, 0, 0, 0, TimeSpan.Zero);
        var first = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var second = Guid.Parse("22222222-2222-2222-2222-222222222222");
        factory.Database.ContactSubmissions.AddRange(
            new ContactSubmission { Id = second, CreatedAt = time },
            new ContactSubmission { Id = Guid.NewGuid(), CreatedAt = time.AddDays(-1) },
            new ContactSubmission { Id = first, CreatedAt = time, Name = "Name", Phone = Phone, Inquiry = "Full inquiry" });

        var firstPage = await client.GetFromJsonAsync<ApiResponse<JsonElement>>(
            ListRoute + "?pageSize=1");
        var submission = Assert.Single(firstPage!.Data.GetProperty("items").EnumerateArray())
            .Deserialize<ContactSubmissionDto>(JsonSerializerOptions.Web)!;
        Assert.Equal(first, submission.Id);
        Assert.Equal("Name", submission.Name);
        Assert.Equal(Phone, submission.Phone);
        Assert.Equal("Full inquiry", submission.Inquiry);
        Assert.Equal(time, submission.CreatedAt);
        Assert.Equal(3, firstPage.Data.GetProperty("totalCount").GetInt32());

        var nextPage = await client.GetFromJsonAsync<ApiResponse<JsonElement>>(
            ListRoute + "?pageNumber=2&pageSize=1");
        Assert.Equal(second, Assert.Single(nextPage!.Data.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());

        var emptyPage = await client.GetFromJsonAsync<ApiResponse<JsonElement>>(
            ListRoute + "?pageNumber=4&pageSize=1");
        Assert.Empty(emptyPage!.Data.GetProperty("items").EnumerateArray());
    }

    [Theory]
    [InlineData("pageNumber=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    public async Task Invalid_pagination_returns_400(string query)
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Credential("AdminOnly"));
        using var response = await client.GetAsync($"{ListRoute}?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> SubmitAsync(HttpClient client, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, SubmitRoute) { Content = JsonContent.Create(body) };
        await HttpTestRequests.AddProofAsync(client, request, "");
        return await client.SendAsync(request);
    }
}
