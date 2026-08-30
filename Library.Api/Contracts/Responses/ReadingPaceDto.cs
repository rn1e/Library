namespace Library.Api.Contracts.Responses;

public sealed record ReadingPaceDto(long BorrowerId, double? AveragePagesPerDay, int LoansConsidered, IReadOnlyList<LoanPaceDto> Breakdown);

public sealed record LoanPaceDto(long BookId, string Title, int Pages, double Days, double PagesPerDay);
