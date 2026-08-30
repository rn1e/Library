namespace Library.Service.Domain.Statistics;

public sealed record BookBorrowStat(long BookId, string Title, string Author, int BorrowCount);
