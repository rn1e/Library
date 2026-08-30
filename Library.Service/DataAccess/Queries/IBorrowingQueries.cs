using Library.Service.Domain.Reading;
using Library.Service.Domain.Statistics;

namespace Library.Service.DataAccess.Queries;

public interface IBorrowingQueries
{
    Task<IReadOnlyList<BookBorrowStat>> GetMostBorrowedAsync(
        int limit, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);

    Task<IReadOnlyList<BorrowerBorrowStat>> GetTopBorrowersAsync(
        int limit, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);

    Task<IReadOnlyList<RelatedBookStat>> GetAlsoBorrowedAsync(
        long bookId, int limit, CancellationToken ct = default);

    Task<IReadOnlyList<CompletedLoan>> GetCompletedLoansAsync(long borrowerId, CancellationToken ct = default);

    Task<bool> BorrowerExistsAsync(long borrowerId, CancellationToken ct = default);

    Task<bool> BookExistsAsync(long bookId, CancellationToken ct = default);
}
