using Library.Api.Contracts.Requests;
using Library.Api.Contracts.Responses;
using Library.Contracts;

using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[ApiController]
[Route("api/loans")]
public class LoansController : ControllerBase
{
    private readonly LibraryService.LibraryServiceClient _client;

    public LoansController(LibraryService.LibraryServiceClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Lends a free copy of a title to a borrower.
    /// </summary>
    /// <remarks>
    /// 409 for the case when there isn't any copy of book.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(LoanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoanDto>> Borrow([FromBody] BorrowBookRequest request, CancellationToken ct)
    {
        var response = await _client.BorrowBookAsync(
            new BorrowRequest { BorrowerId = request.BorrowerId, BookId = request.BookId }, cancellationToken: ct);

        var loan = new LoanDto(response.LoanId, response.BookCopyId, response.BorrowedAtUtc.ToDateTime());

        return StatusCode(StatusCodes.Status201Created, loan);
    }

    /// <summary>
    /// Closes a loan.
    /// </summary>
    [HttpPost("{loanId:long}/return")]
    [ProducesResponseType(typeof(LoanReturnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoanReturnDto>> Return(long loanId, CancellationToken ct)
    {
        var response = await _client.ReturnBookAsync(
            new ReturnRequest { LoanId = loanId }, cancellationToken: ct);

        return Ok(new LoanReturnDto(
            response.LoanId, response.ReturnedAtUtc.ToDateTime(), response.AlreadyReturned));
    }
}
