using Grpc.Core;

using Library.Contracts;

namespace Library.Service.Services;

public class LibraryGrpcService: LibraryService.LibraryServiceBase
{
    public override Task<MostBorrowedResponse> GetMostBorrowedBooks(MostBorrowedRequest request, ServerCallContext context)
    {
        var response = new MostBorrowedResponse();

        response.Books.Add(new BookStat() { BookId = 1, Author = "Author", Title= "Title", BorrowCount = 5});

        return Task.FromResult(response);
    }
}
