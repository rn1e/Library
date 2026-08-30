using System.ComponentModel.DataAnnotations;

namespace Library.Api.Contracts.Requests;

public sealed record BorrowBookRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "borrowerId must be a positive id")]
    public long BorrowerId { get; init; }

    [Range(1, long.MaxValue, ErrorMessage = "bookId must be a positive id")]
    public long BookId { get; init; }
}
