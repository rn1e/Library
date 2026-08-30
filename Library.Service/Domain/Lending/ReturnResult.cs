namespace Library.Service.Domain.Lending;

public sealed record ReturnResult(long LoanId, DateTime ReturnedAt, bool AlreadyReturned);
