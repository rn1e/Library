using Grpc.Core;

using Library.Contracts;

namespace Library.Tests.Integration.Functional;

[Collection(SqlServerCollection.Name)]
public class ReadingPaceRpcTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlServer;
    private string _connectionString = null!;
    private ServiceHostFactory _service = null!;
    private LibraryService.LibraryServiceClient _client = null!;

    public ReadingPaceRpcTests(SqlServerFixture sqlServer)
    {
        _sqlServer = sqlServer;
    }

    public async Task InitializeAsync()
    {
        _connectionString = await _sqlServer.GetDatabaseAsync(SqlServerFixture.ReadOnly);
        _service = new ServiceHostFactory(_connectionString);
        _client = _service.CreateGrpcClient();
    }

    public Task DisposeAsync() => _service.DisposeAsync().AsTask();

    [Fact]
    public async Task GetReadingPace_WhenBorrowerHasCompletedLoans_ReturnsWeightedAverage()
    {
        var alice = await SqlServerFixture.BorrowerIdAsync(_connectionString, "Alice");

        var response = await _client.GetReadingPaceAsync(new ReadingPaceRequest { BorrowerId = alice });

        // 412 pages over 7 days plus 204 over a same-day loan floored to 1: 616 / 8.
        Assert.True(response.HasData);
        Assert.Equal(77.0, response.AveragePagesPerDay);
        Assert.Equal(2, response.LoansConsidered);
    }

    [Fact]
    public async Task GetReadingPace_WhenBorrowerHasCompletedLoans_ReturnsPerLoanBreakdown()
    {
        var alice = await SqlServerFixture.BorrowerIdAsync(_connectionString, "Alice");

        var response = await _client.GetReadingPaceAsync(new ReadingPaceRequest { BorrowerId = alice });

        var solaris = Assert.Single(response.Breakdown, l => l.Title == "Solaris");
        Assert.Equal(1.0, solaris.Days);
        Assert.Equal(204.0, solaris.PagesPerDay);
    }

    [Fact]
    public async Task GetReadingPace_WhenBorrowerHasNoCompletedLoans_ReturnsHasDataFalse()
    {
        var eve = await SqlServerFixture.BorrowerIdAsync(_connectionString, "Eve");

        var response = await _client.GetReadingPaceAsync(new ReadingPaceRequest { BorrowerId = eve });

        Assert.False(response.HasData);
        Assert.Equal(0, response.LoansConsidered);
        Assert.Empty(response.Breakdown);
    }

    [Fact]
    public async Task GetReadingPace_WhenBorrowerIsUnknown_ThrowsNotFound()
    {
        var ex = await Assert.ThrowsAsync<RpcException>(
            () => _client.GetReadingPaceAsync(new ReadingPaceRequest { BorrowerId = 999_999 }).ResponseAsync);

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }
}
