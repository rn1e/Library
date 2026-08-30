namespace Library.Service.Domain.Reading;

public sealed record CompletedLoan(long BookId, string Title, int Pages, DateTime BorrowedAt, DateTime ReturnedAt);
