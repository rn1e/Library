using Google.Protobuf.WellKnownTypes;

using Library.Api.Contracts.Requests;
using Library.Api.Contracts.Responses;
using Library.Contracts;

using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly LibraryService.LibraryServiceClient _client;

    public BooksController(LibraryService.LibraryServiceClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Most borrowed titles, optionally within a period.
    /// </summary>
    [HttpGet("most-borrowed")]
    [ProducesResponseType(typeof(IReadOnlyList<BookStatDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookStatDto>>> GetMostBorrowed(
        [FromQuery] MostBorrowedQuery query, CancellationToken ct)
    {
        var request = new MostBorrowedRequest { Limit = query.Limit };

        if (query.From is { } from)
            request.FromUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(from, DateTimeKind.Utc));

        if (query.To is { } to)
            request.ToUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(to, DateTimeKind.Utc));

        var response = await _client.GetMostBorrowedBooksAsync(request, cancellationToken: ct);

        return Ok(response.Books
            .Select(b => new BookStatDto(b.BookId, b.Title, b.Author, b.BorrowCount))
            .ToList());
    }

    /// <summary>
    /// Titles also borrowed by the people who borrowed this one, ranked by shared readers.
    /// </summary>
    [HttpGet("{bookId:long}/also-borrowed")]
    [ProducesResponseType(typeof(IReadOnlyList<RelatedBookDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RelatedBookDto>>> GetAlsoBorrowed(
        long bookId, [FromQuery] AlsoBorrowedQuery query, CancellationToken ct)
    {
        var response = await _client.GetAlsoBorrowedAsync(
            new AlsoBorrowedRequest { BookId = bookId, Limit = query.Limit }, cancellationToken: ct);

        return Ok(response.Books
            .Select(b => new RelatedBookDto(b.BookId, b.Title, b.Author, b.SharedReaders))
            .ToList());
    }
}
