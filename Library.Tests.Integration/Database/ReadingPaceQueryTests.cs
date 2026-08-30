using Library.Service.DataAccess;
using Library.Service.DataAccess.Queries;

namespace Library.Tests.Integration.Database;

[Collection(SqlServerCollection.Name)]
public class ReadingPaceQueryTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlServer;
    private string _connectionString = null!;
    private LibraryDbContext _db = null!;
    private IBorrowingQueries _queries = null!;

    public ReadingPaceQueryTests(SqlServerFixture sqlServer)
    {
        _sqlServer = sqlServer;
    }

    public async Task InitializeAsync()
    {
        _connectionString = await _sqlServer.GetDatabaseAsync(SqlServerFixture.ReadOnly);
        _db = SqlServerFixture.NewContext(_connectionString);
        _queries = new BorrowingQueries(_db);
    }

    public Task DisposeAsync() => _db.DisposeAsync().AsTask();

    [Fact]
    public async Task GetCompletedLoans_WhenBorrowerHasReturnedBooks_ReturnsThemWithPageCounts()
    {
        var alice = await SqlServerFixture.BorrowerIdAsync(_connectionString, "Alice");

        var loans = await _queries.GetCompletedLoansAsync(alice);

        Assert.Equal(new[] { "Dune", "Solaris" }, loans.Select(l => l.Title));
        Assert.Equal(new[] { 412, 204 }, loans.Select(l => l.Pages));
    }

    [Fact]
    public async Task GetCompletedLoans_WhenBorrowerHasAnOngoingLoan_ExcludesIt()
    {
        var alice = await SqlServerFixture.BorrowerIdAsync(_connectionString, "Alice");

        var loans = await _queries.GetCompletedLoansAsync(alice);

        Assert.DoesNotContain(loans, l => l.Title == "Hyperion");
    }

    [Fact]
    public async Task GetCompletedLoans_WhenBorrowerHasOnlyOngoingLoans_ReturnsEmpty()
    {
        var eve = await SqlServerFixture.BorrowerIdAsync(_connectionString, "Eve");

        var loans = await _queries.GetCompletedLoansAsync(eve);

        Assert.Empty(loans);
    }

    [Fact]
    public async Task GetCompletedLoans_WhenLoansAreRead_TimestampsComeBackAsUtc()
    {
        var alice = await SqlServerFixture.BorrowerIdAsync(_connectionString, "Alice");

        var loans = await _queries.GetCompletedLoansAsync(alice);

        Assert.All(loans, l =>
        {
            Assert.Equal(DateTimeKind.Utc, l.BorrowedAt.Kind);
            Assert.Equal(DateTimeKind.Utc, l.ReturnedAt.Kind);
        });
    }

    [Fact]
    public async Task BorrowerExists_WhenBorrowerIsUnknown_ReturnsFalse()
    {
        Assert.False(await _queries.BorrowerExistsAsync(999_999));
    }

    [Fact]
    public async Task BorrowerExists_WhenBorrowerHasNoLoansAtAll_ReturnsTrue()
    {
        var frank = await SqlServerFixture.BorrowerIdAsync(_connectionString, "Frank");

        Assert.True(await _queries.BorrowerExistsAsync(frank));
        Assert.Empty(await _queries.GetCompletedLoansAsync(frank));
    }
}
