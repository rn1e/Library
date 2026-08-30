namespace Library.Service.Domain.Reading;

public sealed record LoanPace(long BookId, string Title, int Pages, double Days, double PagesPerDay);
