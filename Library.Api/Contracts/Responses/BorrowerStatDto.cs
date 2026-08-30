namespace Library.Api.Contracts.Responses;

public sealed record BorrowerStatDto(long BorrowerId, string FirstName, string LastName, int BorrowCount);
