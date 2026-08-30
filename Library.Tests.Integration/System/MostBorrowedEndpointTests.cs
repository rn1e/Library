using System.Net;
using System.Net.Http.Json;

using Library.Api.Contracts.Responses;

namespace Library.Tests.Integration.System;

/// <summary>
/// System level: HTTP in, gRPC hop, real database, JSON out. Nothing is mocked — if a
/// test at this level needed a stub, it would not be a system test.
/// </summary>
[Collection(SqlServerCollection.Name)]
public class MostBorrowedEndpointTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlServer;
    private ServiceHostFactory _service = null!;
    private ApiHostFactory _api = null!;
    private HttpClient _http = null!;

    public MostBorrowedEndpointTests(SqlServerFixture sqlServer)
    {
        _sqlServer = sqlServer;
    }

    public async Task InitializeAsync()
    {
        _service = new ServiceHostFactory(await _sqlServer.GetDatabaseAsync(SqlServerFixture.ReadOnly));
        _api = new ApiHostFactory(_service);
        _http = _api.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await _api.DisposeAsync();
        await _service.DisposeAsync();
    }

    [Fact]
    public async Task GetMostBorrowed_WhenCalled_ReturnsRankedTitlesAsJson()
    {
        var books = await _http.GetFromJsonAsync<List<BookStatDto>>("/api/books/most-borrowed?limit=3");

        Assert.NotNull(books);
        Assert.Equal(
            new[] { ("Dune", 5), ("Neuromancer", 4), ("Snow Crash", 3) },
            books.Select(b => (b.Title, b.BorrowCount)));
    }

    [Fact]
    public async Task GetMostBorrowed_WhenPeriodInQueryString_AppliesThePeriod()
    {
        var books = await _http.GetFromJsonAsync<List<BookStatDto>>(
            "/api/books/most-borrowed?limit=10&from=2026-08-01T00:00:00Z&to=2026-09-01T00:00:00Z");

        Assert.NotNull(books);
        Assert.Equal(14, books.Sum(b => b.BorrowCount));
    }

    [Fact]
    public async Task GetMostBorrowed_WhenNoLimitGiven_DefaultsToTen()
    {
        var books = await _http.GetFromJsonAsync<List<BookStatDto>>("/api/books/most-borrowed");

        Assert.NotNull(books);
        Assert.Equal(8, books.Count);
    }

    [Fact]
    public async Task GetMostBorrowed_WhenLimitOutOfRange_ReturnsBadRequest()
    {
        var response = await _http.GetAsync("/api/books/most-borrowed?limit=1000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
