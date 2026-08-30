using Library.Service.DataAccess;
using Library.Service.Domain.Entities;
using Library.Service.Domain.Exceptions;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Library.Service.Domain.Lending;

public class LoanService : ILoanService
{
    /// <summary>SQL Server's duplicate-key errors: 2601 for a unique index, 2627 for a unique constraint.</summary>
    private const int DuplicateKey = 2601;
    private const int UniqueConstraintViolation = 2627;

    private readonly LibraryDbContext _db;
    private readonly ILogger<LoanService> _logger;

    public LoanService(LibraryDbContext db, ILogger<LoanService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<LoanResult> BorrowAsync(long borrowerId, long bookId, CancellationToken ct = default)
    {
        if (!await _db.Borrowers.AnyAsync(b => b.Id == borrowerId, ct))
            throw new BorrowerNotFoundException(borrowerId);

        if (!await _db.Books.AnyAsync(b => b.Id == bookId, ct))
            throw new BookNotFoundException(bookId);

        var freeCopy = await _db.BookCopies
            .Where(c => c.BookId == bookId && !c.Loans.Any(l => l.ReturnedAt == null))
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync(ct);

        if (freeCopy is null)
            throw new NoCopiesAvailableException(bookId);

        var loan = new Loan
        {
            BookCopyId = freeCopy.Id,
            BorrowerId = borrowerId,
            BorrowedAt = DateTime.UtcNow,
        };

        _db.Loans.Add(loan);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException { Number: DuplicateKey or UniqueConstraintViolation })
        {
            _logger.LogWarning("Concurrent borrow of copy {BookCopyId} was rejected by IX_Loan_ActiveCopy", freeCopy.Id);

            throw new NoCopiesAvailableException(bookId);
        }

        return new LoanResult(loan.Id, loan.BookCopyId, loan.BorrowedAt);
    }

    public async Task<ReturnResult> ReturnAsync(long loanId, CancellationToken ct = default)
    {
        var loan = await _db.Loans.FirstOrDefaultAsync(l => l.Id == loanId, ct)
                   ?? throw new LoanNotFoundException(loanId);

        if (loan.ReturnedAt is { } alreadyReturnedAt)
            return new ReturnResult(loan.Id, alreadyReturnedAt, AlreadyReturned: true);

        loan.ReturnedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new ReturnResult(loan.Id, loan.ReturnedAt.Value, AlreadyReturned: false);
    }
}
