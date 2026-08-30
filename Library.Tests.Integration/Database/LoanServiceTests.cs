using Library.Service.DataAccess;
using Library.Service.Domain.Entities;
using Library.Service.Domain.Exceptions;
using Library.Service.Domain.Lending;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Library.Tests.Integration.Database;

[Collection(SqlServerCollection.Name)]
public class LoanServiceTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlServer;
    private LibraryDbContext _db = null!;
    private ILoanService _loans = null!;

    public LoanServiceTests(SqlServerFixture sqlServer)
    {
        _sqlServer = sqlServer;
    }

    public async Task InitializeAsync()
    {
        _db = SqlServerFixture.NewContext(await _sqlServer.GetDatabaseAsync("lending"));
        _loans = new LoanService(_db, NullLogger<LoanService>.Instance);
    }

    public Task DisposeAsync() => _db.DisposeAsync().AsTask();

    [Fact]
    public async Task Borrow_WhenACopyIsFree_OpensALoanOnIt()
    {
        var (borrowerId, bookId) = await NewTitleAndReaderAsync();

        var result = await _loans.BorrowAsync(borrowerId, bookId);

        Assert.True(result.LoanId > 0);
        Assert.Equal(DateTimeKind.Utc, result.BorrowedAt.Kind);

        var stored = await _db.Loans.AsNoTracking().SingleAsync(l => l.Id == result.LoanId);
        Assert.Null(stored.ReturnedAt);
        Assert.Equal(result.BookCopyId, stored.BookCopyId);
    }

    [Fact]
    public async Task Borrow_WhenSeveralCopiesAreFree_LendsADifferentOneEachTime()
    {
        var (borrowerId, bookId) = await NewTitleAndReaderAsync(copies: 2);

        var first = await _loans.BorrowAsync(borrowerId, bookId);
        var second = await _loans.BorrowAsync(borrowerId, bookId);

        Assert.NotEqual(first.BookCopyId, second.BookCopyId);
    }

    [Fact]
    public async Task Borrow_WhenEveryCopyIsOnLoan_ThrowsNoCopiesAvailable()
    {
        var (borrowerId, bookId) = await NewTitleAndReaderAsync();
        await _loans.BorrowAsync(borrowerId, bookId);

        await Assert.ThrowsAsync<NoCopiesAvailableException>(() => _loans.BorrowAsync(borrowerId, bookId));
    }

    [Fact]
    public async Task Borrow_WhenBorrowerIsUnknown_ThrowsBorrowerNotFound()
    {
        var (_, bookId) = await NewTitleAndReaderAsync();

        await Assert.ThrowsAsync<BorrowerNotFoundException>(() => _loans.BorrowAsync(999_999, bookId));
    }

    [Fact]
    public async Task Borrow_WhenBookIsUnknown_ThrowsBookNotFound()
    {
        var (borrowerId, _) = await NewTitleAndReaderAsync();

        await Assert.ThrowsAsync<BookNotFoundException>(() => _loans.BorrowAsync(borrowerId, 999_999));
    }

    [Fact]
    public async Task Borrow_WhenTheSameCopyIsLentTwice_TheDatabaseRejectsTheSecondLoan()
    {
        var (borrowerId, bookId) = await NewTitleAndReaderAsync();
        var copyId = await _db.BookCopies.Where(c => c.BookId == bookId).Select(c => c.Id).SingleAsync();

        _db.Loans.Add(new Loan { BookCopyId = copyId, BorrowerId = borrowerId, BorrowedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        _db.Loans.Add(new Loan { BookCopyId = copyId, BorrowerId = borrowerId, BorrowedAt = DateTime.UtcNow });
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());

        var sql = Assert.IsType<SqlException>(ex.InnerException);
        Assert.Contains(sql.Number, new[] { 2601, 2627 });
    }

    [Fact]
    public async Task Borrow_WhenACopyIsReturned_TheCopyBecomesLendableAgain()
    {
        var (borrowerId, bookId) = await NewTitleAndReaderAsync();

        var first = await _loans.BorrowAsync(borrowerId, bookId);
        await Assert.ThrowsAsync<NoCopiesAvailableException>(() => _loans.BorrowAsync(borrowerId, bookId));

        await _loans.ReturnAsync(first.LoanId);

        var second = await _loans.BorrowAsync(borrowerId, bookId);
        Assert.Equal(first.BookCopyId, second.BookCopyId);
    }

    [Fact]
    public async Task Return_WhenLoanIsOpen_ClosesIt()
    {
        var (borrowerId, bookId) = await NewTitleAndReaderAsync();
        var loan = await _loans.BorrowAsync(borrowerId, bookId);

        var result = await _loans.ReturnAsync(loan.LoanId);

        Assert.False(result.AlreadyReturned);
        Assert.Equal(DateTimeKind.Utc, result.ReturnedAt.Kind);

        var storedReturnedAt = await _db.Loans.AsNoTracking()
            .Where(l => l.Id == loan.LoanId).Select(l => l.ReturnedAt).SingleAsync();
        Assert.NotNull(storedReturnedAt);
    }

    [Fact]
    public async Task Return_WhenCalledTwice_KeepsTheOriginalReturnDate()
    {
        var (borrowerId, bookId) = await NewTitleAndReaderAsync();
        var loan = await _loans.BorrowAsync(borrowerId, bookId);

        var first = await _loans.ReturnAsync(loan.LoanId);
        var second = await _loans.ReturnAsync(loan.LoanId);

        Assert.False(first.AlreadyReturned);
        Assert.True(second.AlreadyReturned);
        Assert.Equal(first.ReturnedAt, second.ReturnedAt);
    }

    [Fact]
    public async Task Return_WhenLoanIsUnknown_ThrowsLoanNotFound()
    {
        await Assert.ThrowsAsync<LoanNotFoundException>(() => _loans.ReturnAsync(999_999));
    }

    private async Task<(long BorrowerId, long BookId)> NewTitleAndReaderAsync(int copies = 1)
    {
        var borrower = new Borrower { FirstName = "Test", LastName = "Reader" };
        var book = new Book
        {
            Title = $"Test title {Guid.NewGuid()}",
            Author = "Test Author",
            Pages = 100,
            Copies = Enumerable.Range(0, copies).Select(_ => new BookCopy()).ToList(),
        };

        _db.AddRange(borrower, book);
        await _db.SaveChangesAsync();

        return (borrower.Id, book.Id);
    }
}
