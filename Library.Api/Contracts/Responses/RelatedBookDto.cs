namespace Library.Api.Contracts.Responses;

public sealed record RelatedBookDto(long BookId, string Title, string Author, int SharedReaders);
