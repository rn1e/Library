namespace Library.Service.Domain.Lending;

public sealed record LoanResult(long LoanId, long BookCopyId, DateTime BorrowedAt);
