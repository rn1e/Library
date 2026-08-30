namespace Library.Api.Contracts.Responses;

public sealed record BookStatDto(long BookId, string Title, string Author, int BorrowCount);
