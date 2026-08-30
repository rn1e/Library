using Grpc.Core;

using Google.Protobuf.WellKnownTypes;

using Library.Contracts;
using Library.Service.DataAccess.Queries;
using Library.Service.Domain.Exceptions;
using Library.Service.Domain.Lending;
using Library.Service.Domain.Reading;

namespace Library.Service.Services;

public class LibraryGrpcService : LibraryService.LibraryServiceBase
{
    private const int MaxLimit = 100;

    private readonly IBorrowingQueries _queries;
    private readonly ILoanService _loans;
    private readonly ReadingPaceCalculator _readingPace;

    public LibraryGrpcService(IBorrowingQueries queries, ILoanService loans, ReadingPaceCalculator readingPace)
    {
        _queries = queries;
        _loans = loans;
        _readingPace = readingPace;
    }

    public override async Task<MostBorrowedResponse> GetMostBorrowedBooks(MostBorrowedRequest request, ServerCallContext context)
    {
        var limit = ValidateLimit(request.Limit);

        var stats = await _queries.GetMostBorrowedAsync(
            limit, request.FromUtc.ToDateTimeUtc(), request.ToUtc.ToDateTimeUtc(), context.CancellationToken);

        var response = new MostBorrowedResponse();
        response.Books.AddRange(stats.Select(s => s.ToProto()));

        return response;
    }

    public override async Task<TopBorrowersResponse> GetTopBorrowers(TopBorrowersRequest request, ServerCallContext context)
    {
        var limit = ValidateLimit(request.Limit);

        var stats = await _queries.GetTopBorrowersAsync(
            limit, request.FromUtc.ToDateTimeUtc(), request.ToUtc.ToDateTimeUtc(), context.CancellationToken);

        var response = new TopBorrowersResponse();
        response.Borrowers.AddRange(stats.Select(s => s.ToProto()));

        return response;
    }

    public override async Task<AlsoBorrowedResponse> GetAlsoBorrowed(AlsoBorrowedRequest request, ServerCallContext context)
    {
        var limit = ValidateLimit(request.Limit);

        if (!await _queries.BookExistsAsync(request.BookId, context.CancellationToken))
            throw new BookNotFoundException(request.BookId);

        var stats = await _queries.GetAlsoBorrowedAsync(request.BookId, limit, context.CancellationToken);

        var response = new AlsoBorrowedResponse();
        response.Books.AddRange(stats.Select(s => s.ToProto()));

        return response;
    }

    public override async Task<ReadingPaceResponse> GetReadingPace(ReadingPaceRequest request, ServerCallContext context)
    {
        if (!await _queries.BorrowerExistsAsync(request.BorrowerId, context.CancellationToken))
            throw new BorrowerNotFoundException(request.BorrowerId);

        var loans = await _queries.GetCompletedLoansAsync(request.BorrowerId, context.CancellationToken);

        return _readingPace.Calculate(loans).ToProto(request.BorrowerId);
    }

    public override async Task<BorrowResponse> BorrowBook(BorrowRequest request, ServerCallContext context)
    {
        var loan = await _loans.BorrowAsync(request.BorrowerId, request.BookId, context.CancellationToken);

        return new BorrowResponse
        {
            LoanId = loan.LoanId,
            BookCopyId = loan.BookCopyId,
            BorrowedAtUtc = Timestamp.FromDateTime(loan.BorrowedAt),
        };
    }

    public override async Task<ReturnResponse> ReturnBook(ReturnRequest request, ServerCallContext context)
    {
        var result = await _loans.ReturnAsync(request.LoanId, context.CancellationToken);

        return new ReturnResponse
        {
            LoanId = result.LoanId,
            ReturnedAtUtc = Timestamp.FromDateTime(result.ReturnedAt),
            AlreadyReturned = result.AlreadyReturned,
        };
    }

    private static int ValidateLimit(int limit) => limit is >= 1 and <= MaxLimit
        ? limit
        : throw new RpcException(new Status(StatusCode.InvalidArgument, $"limit must be between 1 and {MaxLimit}"));
}
