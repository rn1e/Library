using System.Net;
using System.Net.Http.Json;

using Library.Api.Contracts.Requests;
using Library.Api.Contracts.Responses;
using Library.Service.Domain.Entities;

namespace Library.Tests.Integration.System;

[Collection(SqlServerCollection.Name)]
public class LendingFlowTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlServer;
    private string _connectionString = null!;
    private ServiceHostFactory _service = null!;
    private ApiHostFactory _api = null!;
    private HttpClient _http = null!;

    public LendingFlowTests(SqlServerFixture sqlServer)
    {
        _sqlServer = sqlServer;
    }

    public async Task InitializeAsync()
    {
        _connectionString = await _sqlServer.GetDatabaseAsync("lending-flow");
        _service = new ServiceHostFactory(_connectionString);
        _api = new ApiHostFactory(_service);
        _http = _api.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await _api.DisposeAsync();
        await _service.DisposeAsync();
    }

    [Fact]
    public async Task BorrowThenReturn_WhenTheLoanIsComplete_ItShowsUpInTheStatistics()
    {
        var (borrowerId, bookId, title) = await NewTitleAndReaderAsync();

        var borrowResponse = await _http.PostAsJsonAsync("/api/loans",
            new BorrowBookRequest { BorrowerId = borrowerId, BookId = bookId });
        Assert.Equal(HttpStatusCode.Created, borrowResponse.StatusCode);

        var loan = await borrowResponse.Content.ReadFromJsonAsync<LoanDto>();
        Assert.NotNull(loan);

        var returnResponse = await _http.PostAsync($"/api/loans/{loan.LoanId}/return", content: null);
        Assert.Equal(HttpStatusCode.OK, returnResponse.StatusCode);

        var mostBorrowed = await _http.GetFromJsonAsync<List<BookStatDto>>("/api/books/most-borrowed?limit=100");
        Assert.NotNull(mostBorrowed);
        Assert.Equal(1, Assert.Single(mostBorrowed, b => b.Title == title).BorrowCount);

        var pace = await _http.GetFromJsonAsync<ReadingPaceDto>($"/api/borrowers/{borrowerId}/reading-pace");
        Assert.NotNull(pace);
        Assert.Equal(1, pace.LoansConsidered);
        Assert.Equal(100.0, pace.AveragePagesPerDay);   // 100 pages, returned the same day
    }

    [Fact]
    public async Task Borrow_WhenTheOnlyCopyIsAlreadyOut_ReturnsConflict()
    {
        var (borrowerId, bookId, _) = await NewTitleAndReaderAsync();
        var request = new BorrowBookRequest { BorrowerId = borrowerId, BookId = bookId };

        await _http.PostAsJsonAsync("/api/loans", request);
        var second = await _http.PostAsJsonAsync("/api/loans", request);

        // 409, not 404: the book exists, it is simply all out.
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Borrow_WhenBookIsUnknown_ReturnsNotFound()
    {
        var (borrowerId, _, _) = await NewTitleAndReaderAsync();

        var response = await _http.PostAsJsonAsync("/api/loans",
            new BorrowBookRequest { BorrowerId = borrowerId, BookId = 999_999 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Borrow_WhenIdsAreNotPositive_ReturnsBadRequest()
    {
        var response = await _http.PostAsJsonAsync("/api/loans",
            new BorrowBookRequest { BorrowerId = 0, BookId = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Return_WhenCalledTwice_StaysSuccessful()
    {
        var (borrowerId, bookId, _) = await NewTitleAndReaderAsync();

        var borrowResponse = await _http.PostAsJsonAsync("/api/loans",
            new BorrowBookRequest { BorrowerId = borrowerId, BookId = bookId });
        var loan = await borrowResponse.Content.ReadFromJsonAsync<LoanDto>();
        Assert.NotNull(loan);

        await _http.PostAsync($"/api/loans/{loan.LoanId}/return", content: null);
        var second = await _http.PostAsync($"/api/loans/{loan.LoanId}/return", content: null);

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var result = await second.Content.ReadFromJsonAsync<LoanReturnDto>();
        Assert.NotNull(result);
        Assert.True(result.AlreadyReturned);
    }

    private async Task<(long BorrowerId, long BookId, string Title)> NewTitleAndReaderAsync()
    {
        await using var db = SqlServerFixture.NewContext(_connectionString);

        var borrower = new Borrower { FirstName = "Test", LastName = "Reader" };
        var book = new Book
        {
            Title = $"Test title {Guid.NewGuid()}",
            Author = "Test Author",
            Pages = 100,
            Copies = new List<BookCopy> { new() },
        };

        db.AddRange(borrower, book);
        await db.SaveChangesAsync();

        return (borrower.Id, book.Id, book.Title);
    }
}
