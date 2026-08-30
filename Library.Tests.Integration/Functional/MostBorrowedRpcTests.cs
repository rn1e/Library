using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using Library.Contracts;
using Library.Service.DataAccess;

namespace Library.Tests.Integration.Functional;

[Collection(SqlServerCollection.Name)]
public class MostBorrowedRpcTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlServer;
    private ServiceHostFactory _service = null!;
    private LibraryService.LibraryServiceClient _client = null!;

    public MostBorrowedRpcTests(SqlServerFixture sqlServer)
    {
        _sqlServer = sqlServer;
    }

    public async Task InitializeAsync()
    {
        _service = new ServiceHostFactory(await _sqlServer.GetDatabaseAsync(SqlServerFixture.ReadOnly));
        _client = _service.CreateGrpcClient();
    }

    public Task DisposeAsync() => _service.DisposeAsync().AsTask();

    [Fact]
    public async Task GetMostBorrowedBooks_WhenCalled_ReturnsRankedTitles()
    {
        var response = await _client.GetMostBorrowedBooksAsync(new MostBorrowedRequest { Limit = 3 });

        Assert.Equal(
            new[] { "Dune", "Neuromancer", "Snow Crash" },
            response.Books.Select(b => b.Title));
        Assert.Equal("Frank Herbert", response.Books[0].Author);
    }

    [Fact]
    public async Task GetMostBorrowedBooks_WhenBothBoundsSet_AppliesThePeriod()
    {
        var response = await _client.GetMostBorrowedBooksAsync(new MostBorrowedRequest
        {
            Limit = 10,
            FromUtc = Timestamp.FromDateTime(Seed.WindowFrom),
            ToUtc = Timestamp.FromDateTime(Seed.WindowTo),
        });

        Assert.Equal(14, response.Books.Sum(b => b.BorrowCount));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task GetMostBorrowedBooks_WhenLimitOutOfRange_ThrowsInvalidArgument(int limit)
    {
        var ex = await Assert.ThrowsAsync<RpcException>(
            () => _client.GetMostBorrowedBooksAsync(new MostBorrowedRequest { Limit = limit }).ResponseAsync);

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
    }
}
