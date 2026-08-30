namespace Library.Service.Domain.Lending;

public interface ILoanService
{
    Task<LoanResult> BorrowAsync(long borrowerId, long bookId, CancellationToken ct = default);

    Task<ReturnResult> ReturnAsync(long loanId, CancellationToken ct = default);
}
