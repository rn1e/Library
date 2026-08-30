using Library.Service.Domain.Reading;
using Library.Service.Domain.Statistics;

using Microsoft.EntityFrameworkCore;

namespace Library.Service.DataAccess.Queries;

public class BorrowingQueries : IBorrowingQueries
{
    private readonly LibraryDbContext _db;

    public BorrowingQueries(LibraryDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<BookBorrowStat>> GetMostBorrowedAsync(
        int limit, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        var loans = _db.Loans.AsNoTracking();

        if (fromUtc is { } from)
            loans = loans.Where(l => l.BorrowedAt >= from);

        if (toUtc is { } to)
            loans = loans.Where(l => l.BorrowedAt < to);

        var rows = await loans
            .GroupBy(l => new { l.BookCopy.BookId, l.BookCopy.Book.Title, l.BookCopy.Book.Author })
            .Select(g => new { g.Key.BookId, g.Key.Title, g.Key.Author, BorrowCount = g.Count() })
            .OrderByDescending(r => r.BorrowCount)
            .ThenBy(r => r.BookId)
            .Take(limit)
            .ToListAsync(ct);

        return rows
            .Select(r => new BookBorrowStat(r.BookId, r.Title, r.Author, r.BorrowCount))
            .ToList();
    }

    public async Task<IReadOnlyList<BorrowerBorrowStat>> GetTopBorrowersAsync(
        int limit, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        var loans = _db.Loans.AsNoTracking();

        if (fromUtc is { } from)
            loans = loans.Where(l => l.BorrowedAt >= from);

        if (toUtc is { } to)
            loans = loans.Where(l => l.BorrowedAt < to);

        var rows = await loans
            .GroupBy(l => new { l.BorrowerId, l.Borrower.FirstName, l.Borrower.LastName })
            .Select(g => new { g.Key.BorrowerId, g.Key.FirstName, g.Key.LastName, BorrowCount = g.Count() })
            .OrderByDescending(r => r.BorrowCount)
            .ThenBy(r => r.BorrowerId)
            .Take(limit)
            .ToListAsync(ct);

        return rows
            .Select(r => new BorrowerBorrowStat(r.BorrowerId, r.FirstName, r.LastName, r.BorrowCount))
            .ToList();
    }

    public async Task<IReadOnlyList<RelatedBookStat>> GetAlsoBorrowedAsync(
        long bookId, int limit, CancellationToken ct = default)
    {
        var readers = _db.Loans
            .Where(l => l.BookCopy.BookId == bookId)
            .Select(l => l.BorrowerId);

        var rows = await _db.Loans.AsNoTracking()
            .Where(l => readers.Contains(l.BorrowerId) && l.BookCopy.BookId != bookId)
            .GroupBy(l => new { l.BookCopy.BookId, l.BookCopy.Book.Title, l.BookCopy.Book.Author })
            .Select(g => new
            {
                g.Key.BookId,
                g.Key.Title,
                g.Key.Author,
                SharedReaders = g.Select(l => l.BorrowerId).Distinct().Count(),
            })
            .OrderByDescending(r => r.SharedReaders)
            .ThenBy(r => r.BookId)
            .Take(limit)
            .ToListAsync(ct);

        return rows
            .Select(r => new RelatedBookStat(r.BookId, r.Title, r.Author, r.SharedReaders))
            .ToList();
    }

    public async Task<IReadOnlyList<CompletedLoan>> GetCompletedLoansAsync(long borrowerId, CancellationToken ct = default)
    {
        var rows = await _db.Loans.AsNoTracking()
            .Where(l => l.BorrowerId == borrowerId && l.ReturnedAt != null)
            .OrderBy(l => l.BorrowedAt)
            .Select(l => new
            {
                l.BookCopy.BookId,
                l.BookCopy.Book.Title,
                l.BookCopy.Book.Pages,
                l.BorrowedAt,
                ReturnedAt = l.ReturnedAt!.Value,
            })
            .ToListAsync(ct);

        return rows
            .Select(r => new CompletedLoan(r.BookId, r.Title, r.Pages, r.BorrowedAt, r.ReturnedAt))
            .ToList();
    }

    public Task<bool> BorrowerExistsAsync(long borrowerId, CancellationToken ct = default) =>
        _db.Borrowers.AsNoTracking().AnyAsync(b => b.Id == borrowerId, ct);

    public Task<bool> BookExistsAsync(long bookId, CancellationToken ct = default) =>
        _db.Books.AsNoTracking().AnyAsync(b => b.Id == bookId, ct);
}
