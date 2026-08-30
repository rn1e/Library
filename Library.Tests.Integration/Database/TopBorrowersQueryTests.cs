using Library.Service.DataAccess;
using Library.Service.DataAccess.Queries;

namespace Library.Tests.Integration.Database;

[Collection(SqlServerCollection.Name)]
public class TopBorrowersQueryTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlServer;
    private LibraryDbContext _db = null!;
    private IBorrowingQueries _queries = null!;

    public TopBorrowersQueryTests(SqlServerFixture sqlServer)
    {
        _sqlServer = sqlServer;
    }

    public async Task InitializeAsync()
    {
        _db = SqlServerFixture.NewContext(await _sqlServer.GetDatabaseAsync(SqlServerFixture.ReadOnly));
        _queries = new BorrowingQueries(_db);
    }

    public Task DisposeAsync() => _db.DisposeAsync().AsTask();

    [Fact]
    public async Task GetTopBorrowers_WhenNoPeriodGiven_RanksOverTheWholeHistory()
    {
        var result = await _queries.GetTopBorrowersAsync(2, null, null);

        Assert.Equal(
            new[] { ("Bob", 7), ("Clara", 5) },
            result.Select(b => (b.FirstName, b.BorrowCount)));
    }

    [Fact]
    public async Task GetTopBorrowers_WhenPeriodGiven_ExcludesLoansStartedBeforeIt()
    {
        var result = await _queries.GetTopBorrowersAsync(1, Seed.WindowFrom, Seed.WindowTo);

        var bob = Assert.Single(result);
        Assert.Equal("Bob", bob.FirstName);
        Assert.Equal(4, bob.BorrowCount);
    }

    [Fact]
    public async Task GetTopBorrowers_WhenLoanIsStillOpen_StillCountsIt()
    {
        var result = await _queries.GetTopBorrowersAsync(100, Seed.WindowFrom, Seed.WindowTo);

        var eve = Assert.Single(result, b => b.FirstName == "Eve");
        Assert.Equal(1, eve.BorrowCount);
    }

    [Fact]
    public async Task GetTopBorrowers_WhenBorrowerNeverBorrowed_ExcludesThem()
    {
        var result = await _queries.GetTopBorrowersAsync(100, null, null);

        Assert.DoesNotContain(result, b => b.FirstName == "Frank");
    }

    [Fact]
    public async Task GetTopBorrowers_WhenPeriodIsEmpty_ReturnsNothing()
    {
        var before = Seed.Anchor.AddYears(-1);

        var result = await _queries.GetTopBorrowersAsync(10, before, before.AddDays(1));

        Assert.Empty(result);
    }
}
