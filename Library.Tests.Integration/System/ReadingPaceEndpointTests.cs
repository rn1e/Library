using System.Net;
using System.Net.Http.Json;

using Library.Api.Contracts.Responses;

namespace Library.Tests.Integration.System;

[Collection(SqlServerCollection.Name)]
public class ReadingPaceEndpointTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlServer;
    private string _connectionString = null!;
    private ServiceHostFactory _service = null!;
    private ApiHostFactory _api = null!;
    private HttpClient _http = null!;

    public ReadingPaceEndpointTests(SqlServerFixture sqlServer)
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
    public async Task GetReadingPace_WhenBorrowerHasCompletedLoans_ReturnsPaceAndBreakdown()
    {
        var alice = await SqlServerFixture.BorrowerIdAsync(_connectionString, "Alice");

        var pace = await _http.GetFromJsonAsync<ReadingPaceDto>($"/api/borrowers/{alice}/reading-pace");

        Assert.NotNull(pace);
        Assert.Equal(77.0, pace.AveragePagesPerDay);
        Assert.Equal(2, pace.LoansConsidered);
        Assert.Equal(new[] { "Dune", "Solaris" }, pace.Breakdown.Select(l => l.Title));
    }

    [Fact]
    public async Task GetReadingPace_WhenBorrowerHasNoCompletedLoans_ReturnsNullAverage()
    {
        var eve = await SqlServerFixture.BorrowerIdAsync(_connectionString, "Eve");

        var pace = await _http.GetFromJsonAsync<ReadingPaceDto>($"/api/borrowers/{eve}/reading-pace");

        Assert.NotNull(pace);
        Assert.Null(pace.AveragePagesPerDay);
        Assert.Empty(pace.Breakdown);
    }

    [Fact]
    public async Task GetReadingPace_WhenBorrowerIsUnknown_ReturnsNotFound()
    {
        var response = await _http.GetAsync("/api/borrowers/999999/reading-pace");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
