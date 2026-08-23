using Library.Contracts;

using Microsoft.AspNetCore.Mvc;

namespace Library.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BooksController : ControllerBase
{
    private readonly LibraryService.LibraryServiceClient _libraryServiceClient;

    public BooksController(LibraryService.LibraryServiceClient libraryServiceClient)
    {
        _libraryServiceClient = libraryServiceClient;
    }

    [HttpGet("most-borrowed")]
    public async Task<ActionResult> GetMostBorrowedBooks()
    {
        var response = await _libraryServiceClient.GetMostBorrowedBooksAsync(new MostBorrowedRequest { Limit = 1 }, null);

        return Ok(response.Books.Select(x => new { Book = x.BookId }));
    }

}
