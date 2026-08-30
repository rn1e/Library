namespace Library.Service.Domain.Reading;

public class ReadingPaceCalculator
{
    public ReadingPace? Calculate(IReadOnlyList<CompletedLoan> loans)
    {
        if (loans.Count == 0)
            return null;

        var stats = loans.Select(x =>
        {
            var days = Math.Max((x.ReturnedAt - x.BorrowedAt).TotalDays, 1.0);
            return new LoanPace(x.BookId, x.Title, x.Pages, days, x.Pages / days);
        }).ToList();

        var average = stats.Sum(x => x.Pages) / stats.Sum(x => x.Days);

        return new ReadingPace(average, stats);
    }
}
