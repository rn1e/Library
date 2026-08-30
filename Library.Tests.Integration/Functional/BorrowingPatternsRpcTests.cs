using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using Library.Contracts;
using Library.Service.DataAccess;

namespace Library.Tests.Integration.Functional;

[Collection(SqlServerCollection.Name)]
public class BorrowingPatternsRpcTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlServer;
    private string _connectionString = null!;
    private ServiceHostFactory _service = null!;
    private LibraryService.LibraryServiceClient _client = null!;

    public BorrowingPatternsRpcTests(SqlServerFixture sqlServer)
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
    public async Task GetTopBorrowers_WhenPeriodGiven_ReturnsRankedBorrowers()
    {
        var response = await _client.GetTopBorrowersAsync(new TopBorrowersRequest
        {
            Limit = 1,
            FromUtc = Timestamp.FromDateTime(Seed.WindowFrom),
            ToUtc = Timestamp.FromDateTime(Seed.WindowTo),
        });

        var bob = Assert.Single(response.Borrowers);
        Assert.Equal("Bob", bob.FirstName);
        Assert.Equal("Marsden", bob.LastName);
        Assert.Equal(4, bob.BorrowCount);
    }

    [Fact]
    public async Task GetTopBorrowers_WhenLimitOutOfRange_ThrowsInvalidArgument()
    {
        var ex = await Assert.ThrowsAsync<RpcException>(
            () => _client.GetTopBorrowersAsync(new TopBorrowersRequest { Limit = 0 }).ResponseAsync);

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
    }

    [Fact]
    public async Task GetAlsoBorrowed_WhenBookHasReaders_ReturnsRankedTitles()
    {
        var dune = await SqlServerFixture.BookIdAsync(_connectionString, "Dune");

        var response = await _client.GetAlsoBorrowedAsync(new AlsoBorrowedRequest { BookId = dune, Limit = 2 });

        Assert.Equal(new[] { "Neuromancer", "Snow Crash" }, response.Books.Select(b => b.Title));
        Assert.Equal(3, response.Books[0].SharedReaders);
    }

    [Fact]
    public async Task GetAlsoBorrowed_WhenBookIsUnknown_ThrowsNotFound()
    {
        var ex = await Assert.ThrowsAsync<RpcException>(
            () => _client.GetAlsoBorrowedAsync(new AlsoBorrowedRequest { BookId = 999_999, Limit = 10 }).ResponseAsync);

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }
}
