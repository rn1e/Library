namespace Library.Service.Domain.Statistics;

public sealed record BorrowerBorrowStat(long BorrowerId, string FirstName, string LastName, int BorrowCount);
