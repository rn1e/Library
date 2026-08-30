using Library.Service.DataAccess;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using Testcontainers.MsSql;

namespace Library.Tests.Integration;

public sealed class SqlServerFixture : IAsyncLifetime
{
    public const string ReadOnly = "readonly";

    private readonly MsSqlContainer _container = new MsSqlBuilder().Build();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, string> _databases = new();
    private int _databaseCount;

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public async Task<string> GetDatabaseAsync(string key)
    {
        await _gate.WaitAsync();
        try
        {
            if (!_databases.TryGetValue(key, out var connectionString))
                _databases[key] = connectionString = await CreateDatabaseAsync();

            return connectionString;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<string> CreateDatabaseAsync()
    {
        var connectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = $"Library_{Interlocked.Increment(ref _databaseCount)}",
        }.ConnectionString;

        await using var db = NewContext(connectionString);
        await db.Database.MigrateAsync();
        await Seed.ApplyAsync(db);

        return connectionString;
    }

    public static LibraryDbContext NewContext(string connectionString) =>
        new(new DbContextOptionsBuilder<LibraryDbContext>().UseSqlServer(connectionString).Options);

    public static async Task<long> BorrowerIdAsync(string connectionString, string firstName)
    {
        await using var db = NewContext(connectionString);

        return await db.Borrowers.Where(b => b.FirstName == firstName).Select(b => b.Id).SingleAsync();
    }

    public static async Task<long> BookIdAsync(string connectionString, string title)
    {
        await using var db = NewContext(connectionString);

        return await db.Books.Where(b => b.Title == title).Select(b => b.Id).SingleAsync();
    }
}

[CollectionDefinition(Name)]
public class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SqlServer";
}
