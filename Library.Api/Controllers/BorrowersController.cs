using Google.Protobuf.WellKnownTypes;

using Library.Api.Contracts.Requests;
using Library.Api.Contracts.Responses;
using Library.Contracts;

using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/borrowers")]
public class BorrowersController : ControllerBase
{
    private readonly LibraryService.LibraryServiceClient _client;

    public BorrowersController(LibraryService.LibraryServiceClient client)
    {
        _client = client;
    }

    /// <summary>
    /// The most active borrowers, optionally within a period.
    /// </summary>
    [HttpGet("top")]
    [ProducesResponseType(typeof(IReadOnlyList<BorrowerStatDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BorrowerStatDto>>> GetTopBorrowers(
        [FromQuery] TopBorrowersQuery query, CancellationToken ct)
    {
        var request = new TopBorrowersRequest { Limit = query.Limit };

        if (query.From is { } from)
            request.FromUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(from, DateTimeKind.Utc));

        if (query.To is { } to)
            request.ToUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(to, DateTimeKind.Utc));

        var response = await _client.GetTopBorrowersAsync(request, cancellationToken: ct);

        return Ok(response.Borrowers
            .Select(b => new BorrowerStatDto(b.BorrowerId, b.FirstName, b.LastName, b.BorrowCount))
            .ToList());
    }

    /// <summary>
    /// A borrower's reading pace in pages per day, with the per-loan breakdown it was derived from.
    /// </summary>
    [HttpGet("{borrowerId:long}/reading-pace")]
    [ProducesResponseType(typeof(ReadingPaceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadingPaceDto>> GetReadingPace(long borrowerId, CancellationToken ct)
    {
        var response = await _client.GetReadingPaceAsync(
            new ReadingPaceRequest { BorrowerId = borrowerId }, cancellationToken: ct);

        return Ok(new ReadingPaceDto(
            response.BorrowerId,
            response.HasData ? response.AveragePagesPerDay : null,
            response.LoansConsidered,
            response.Breakdown
                .Select(l => new LoanPaceDto(l.BookId, l.Title, l.Pages, l.Days, l.PagesPerDay))
                .ToList()));
    }
}
