namespace Library.Service.Domain.Statistics;

public sealed record RelatedBookStat(long BookId, string Title, string Author, int SharedReaders);
