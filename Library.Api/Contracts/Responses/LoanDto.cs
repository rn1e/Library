namespace Library.Api.Contracts.Responses;

public sealed record LoanDto(long LoanId, long BookCopyId, DateTime BorrowedAtUtc);

public sealed record LoanReturnDto(long LoanId, DateTime ReturnedAtUtc, bool AlreadyReturned);
