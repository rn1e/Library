using Grpc.Core;

using Library.Contracts;
using Library.Service.Domain.Entities;

namespace Library.Tests.Integration.Functional;

[Collection(SqlServerCollection.Name)]
public class LendingRpcTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlServer;
    private string _connectionString = null!;
    private ServiceHostFactory _service = null!;
    private LibraryService.LibraryServiceClient _client = null!;

    public LendingRpcTests(SqlServerFixture sqlServer)
    {
        _sqlServer = sqlServer;
    }

    public async Task InitializeAsync()
    {
        _connectionString = await _sqlServer.GetDatabaseAsync("lending-rpc");
        _service = new ServiceHostFactory(_connectionString);
        _client = _service.CreateGrpcClient();
    }

    public Task DisposeAsync() => _service.DisposeAsync().AsTask();

    [Fact]
    public async Task BorrowBook_WhenACopyIsFree_ReturnsTheLoan()
    {
        var (borrowerId, bookId) = await NewTitleAndReaderAsync();

        var response = await _client.BorrowBookAsync(new BorrowRequest { BorrowerId = borrowerId, BookId = bookId });

        Assert.True(response.LoanId > 0);
        Assert.True(response.BookCopyId > 0);
        Assert.NotNull(response.BorrowedAtUtc);
    }

    [Fact]
    public async Task BorrowBook_WhenEveryCopyIsOnLoan_ThrowsFailedPrecondition()
    {
        var (borrowerId, bookId) = await NewTitleAndReaderAsync();
        await _client.BorrowBookAsync(new BorrowRequest { BorrowerId = borrowerId, BookId = bookId });

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => _client.BorrowBookAsync(new BorrowRequest { BorrowerId = borrowerId, BookId = bookId }).ResponseAsync);

        Assert.Equal(StatusCode.FailedPrecondition, ex.StatusCode);
    }

    [Fact]
    public async Task BorrowBook_WhenBorrowerIsUnknown_ThrowsNotFound()
    {
        var (_, bookId) = await NewTitleAndReaderAsync();

        var ex = await Assert.ThrowsAsync<RpcException>(
            () => _client.BorrowBookAsync(new BorrowRequest { BorrowerId = 999_999, BookId = bookId }).ResponseAsync);

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task ReturnBook_WhenCalledTwice_ReportsAlreadyReturned()
    {
        var (borrowerId, bookId) = await NewTitleAndReaderAsync();
        var loan = await _client.BorrowBookAsync(new BorrowRequest { BorrowerId = borrowerId, BookId = bookId });

        var first = await _client.ReturnBookAsync(new ReturnRequest { LoanId = loan.LoanId });
        var second = await _client.ReturnBookAsync(new ReturnRequest { LoanId = loan.LoanId });

        Assert.False(first.AlreadyReturned);
        Assert.True(second.AlreadyReturned);
        Assert.Equal(first.ReturnedAtUtc, second.ReturnedAtUtc);
    }

    [Fact]
    public async Task ReturnBook_WhenLoanIsUnknown_ThrowsNotFound()
    {
        var ex = await Assert.ThrowsAsync<RpcException>(
            () => _client.ReturnBookAsync(new ReturnRequest { LoanId = 999_999 }).ResponseAsync);

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    private async Task<(long BorrowerId, long BookId)> NewTitleAndReaderAsync()
    {
        await using var db = SqlServerFixture.NewContext(_connectionString);

        var borrower = new Borrower { FirstName = "Test", LastName = "Reader" };
        var book = new Book
        {
            Title = $"Test title {Guid.NewGuid()}",
            Author = "Test Author",
            Pages = 100,
            Copies = new List<BookCopy> { new() },
        };

        db.AddRange(borrower, book);
        await db.SaveChangesAsync();

        return (borrower.Id, book.Id);
    }
}
