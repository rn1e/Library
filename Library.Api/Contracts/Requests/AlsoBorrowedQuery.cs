using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Contracts.Requests;

public sealed record AlsoBorrowedQuery
{
    [FromQuery(Name = "limit")]
    [Range(1, 100, ErrorMessage = "limit must be between 1 and 100")]
    public int Limit { get; init; } = 10;
}
