using Google.Protobuf.WellKnownTypes;

using Library.Contracts;
using Library.Service.Domain.Reading;
using Library.Service.Domain.Statistics;

using ProtoLoanPace = Library.Contracts.LoanPace;

namespace Library.Service.Services;

internal static class ProtoMappings
{
    public static DateTime? ToDateTimeUtc(this Timestamp? timestamp) => timestamp?.ToDateTime();

    public static BookStat ToProto(this BookBorrowStat stat) => new()
    {
        BookId = stat.BookId,
        Title = stat.Title,
        Author = stat.Author,
        BorrowCount = stat.BorrowCount,
    };

    public static BorrowerStat ToProto(this BorrowerBorrowStat stat) => new()
    {
        BorrowerId = stat.BorrowerId,
        FirstName = stat.FirstName,
        LastName = stat.LastName,
        BorrowCount = stat.BorrowCount,
    };

    public static RelatedBook ToProto(this RelatedBookStat stat) => new()
    {
        BookId = stat.BookId,
        Title = stat.Title,
        Author = stat.Author,
        SharedReaders = stat.SharedReaders,
    };

    public static ReadingPaceResponse ToProto(this ReadingPace? pace, long borrowerId)
    {
        var response = new ReadingPaceResponse { BorrowerId = borrowerId, HasData = pace is not null };

        if (pace is null)
            return response;

        response.AveragePagesPerDay = pace.AveragePagesPerDay;
        response.LoansConsidered = pace.Breakdown.Count;
        response.Breakdown.AddRange(pace.Breakdown.Select(l => new ProtoLoanPace
        {
            BookId = l.BookId,
            Title = l.Title,
            Pages = l.Pages,
            Days = l.Days,
            PagesPerDay = l.PagesPerDay,
        }));

        return response;
    }
}
