using Library.Service.DataAccess;
using Library.Service.DataAccess.Queries;

namespace Library.Tests.Integration.Database;

[Collection(SqlServerCollection.Name)]
public class MostBorrowedQueryTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlServer;
    private LibraryDbContext _db = null!;
    private IBorrowingQueries _queries = null!;

    public MostBorrowedQueryTests(SqlServerFixture sqlServer)
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
    public async Task GetMostBorrowed_WhenNoPeriodGiven_RanksTitlesByLoanCount()
    {
        var result = await _queries.GetMostBorrowedAsync(limit: 3, fromUtc: null, toUtc: null);

        Assert.Equal(
            new[] { ("Dune", 5), ("Neuromancer", 4), ("Snow Crash", 3) },
            result.Select(x => (x.Title, x.BorrowCount)));
    }

    [Fact]
    public async Task GetMostBorrowed_WhenLoansSpreadAcrossCopies_CountsTheTitle()
    {
        var result = await _queries.GetMostBorrowedAsync(limit: 1, fromUtc: null, toUtc: null);

        var dune = Assert.Single(result);
        Assert.Equal(5, dune.BorrowCount);
    }

    [Fact]
    public async Task GetMostBorrowed_WhenPeriodGiven_FiltersOnBorrowDate()
    {
        var all = await _queries.GetMostBorrowedAsync(10, null, null);
        var window = await _queries.GetMostBorrowedAsync(10, Seed.WindowFrom, Seed.WindowTo);

        Assert.Equal(19, all.Sum(x => x.BorrowCount));
        Assert.Equal(14, window.Sum(x => x.BorrowCount));
    }

    [Fact]
    public async Task GetMostBorrowed_WhenTitleNeverBorrowed_ExcludesIt()
    {
        var result = await _queries.GetMostBorrowedAsync(limit: 100, fromUtc: null, toUtc: null);

        Assert.DoesNotContain(result, x => x.Title == "A Fire Upon the Deep");
    }

    [Fact]
    public async Task GetMostBorrowed_WhenLimitGiven_CapsResultCount()
    {
        var result = await _queries.GetMostBorrowedAsync(limit: 2, fromUtc: null, toUtc: null);

        Assert.Equal(2, result.Count);
    }
}
