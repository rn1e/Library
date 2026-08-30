using System.Net;
using System.Net.Http.Json;

using Library.Api.Contracts.Responses;

namespace Library.Tests.Integration.System;

[Collection(SqlServerCollection.Name)]
public class BorrowingPatternsEndpointTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlServer;
    private string _connectionString = null!;
    private ServiceHostFactory _service = null!;
    private ApiHostFactory _api = null!;
    private HttpClient _http = null!;

    public BorrowingPatternsEndpointTests(SqlServerFixture sqlServer)
    {
        _sqlServer = sqlServer;
    }

    public async Task InitializeAsync()
    {
        _connectionString = await _sqlServer.GetDatabaseAsync(SqlServerFixture.ReadOnly);
        _service = new ServiceHostFactory(_connectionString);
        _api = new ApiHostFactory(_service);
        _http = _api.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await _api.DisposeAsync();
        await _service.DisposeAsync();
    }

    [Fact]
    public async Task GetTopBorrowers_WhenPeriodInQueryString_ReturnsRankedBorrowers()
    {
        var borrowers = await _http.GetFromJsonAsync<List<BorrowerStatDto>>(
            "/api/borrowers/top?limit=2&from=2026-08-01T00:00:00Z&to=2026-09-01T00:00:00Z");

        Assert.NotNull(borrowers);
        Assert.Equal("Bob", borrowers[0].FirstName);
        Assert.Equal(4, borrowers[0].BorrowCount);
    }

    [Fact]
    public async Task GetTopBorrowers_WhenNoPeriodGiven_CoversTheWholeHistory()
    {
        var borrowers = await _http.GetFromJsonAsync<List<BorrowerStatDto>>("/api/borrowers/top?limit=1");

        Assert.NotNull(borrowers);
        Assert.Equal(7, Assert.Single(borrowers).BorrowCount);
    }

    [Fact]
    public async Task GetAlsoBorrowed_WhenBookHasReaders_ReturnsRankedTitles()
    {
        var dune = await SqlServerFixture.BookIdAsync(_connectionString, "Dune");

        var related = await _http.GetFromJsonAsync<List<RelatedBookDto>>($"/api/books/{dune}/also-borrowed?limit=2");

        Assert.NotNull(related);
        Assert.Equal(new[] { "Neuromancer", "Snow Crash" }, related.Select(b => b.Title));
        Assert.DoesNotContain(related, b => b.BookId == dune);
    }

    [Fact]
    public async Task GetAlsoBorrowed_WhenBookIsUnknown_ReturnsNotFound()
    {
        var response = await _http.GetAsync("/api/books/999999/also-borrowed");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
