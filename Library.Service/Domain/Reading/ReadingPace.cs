namespace Library.Service.Domain.Reading;

public sealed record ReadingPace(double AveragePagesPerDay, IReadOnlyList<LoanPace> Breakdown);
