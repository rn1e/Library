using Library.Service.DataAccess;
using Library.Service.DataAccess.Queries;

namespace Library.Tests.Integration.Database;

[Collection(SqlServerCollection.Name)]
public class AlsoBorrowedQueryTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlServer;
    private string _connectionString = null!;
    private LibraryDbContext _db = null!;
    private IBorrowingQueries _queries = null!;

    public AlsoBorrowedQueryTests(SqlServerFixture sqlServer)
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
    public async Task GetAlsoBorrowed_WhenBookHasReaders_RanksTitlesBySharedReaderCount()
    {
        var dune = await SqlServerFixture.BookIdAsync(_connectionString, "Dune");

        var result = await _queries.GetAlsoBorrowedAsync(dune, limit: 1);

        var top = Assert.Single(result);
        Assert.Equal("Neuromancer", top.Title);
        Assert.Equal(3, top.SharedReaders);
    }

    [Fact]
    public async Task GetAlsoBorrowed_WhenBookHasReaders_ExcludesTheRequestedBook()
    {
        var dune = await SqlServerFixture.BookIdAsync(_connectionString, "Dune");

        var result = await _queries.GetAlsoBorrowedAsync(dune, limit: 100);

        Assert.DoesNotContain(result, b => b.BookId == dune);
    }

    [Fact]
    public async Task GetAlsoBorrowed_WhenAReaderBorrowedTheSameTitleTwice_CountsThemOnce()
    {
        var dune = await SqlServerFixture.BookIdAsync(_connectionString, "Dune");

        var result = await _queries.GetAlsoBorrowedAsync(dune, limit: 100);

        Assert.Equal(3, Assert.Single(result, b => b.Title == "Neuromancer").SharedReaders);
        Assert.Equal(2, Assert.Single(result, b => b.Title == "Snow Crash").SharedReaders);
    }

    [Fact]
    public async Task GetAlsoBorrowed_WhenNobodyBorrowedTheBook_ReturnsEmpty()
    {
        var fire = await SqlServerFixture.BookIdAsync(_connectionString, "A Fire Upon the Deep");

        var result = await _queries.GetAlsoBorrowedAsync(fire, limit: 10);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAlsoBorrowed_WhenLimitGiven_CapsResultCount()
    {
        var dune = await SqlServerFixture.BookIdAsync(_connectionString, "Dune");

        var result = await _queries.GetAlsoBorrowedAsync(dune, limit: 2);

        Assert.Equal(2, result.Count);
    }
}
