using Library.Service.Domain.Reading;

namespace Library.Tests.Unit;

public class ReadingPaceCalculatorTests
{
    private static readonly DateTime Anchor = new(2026, 08, 23, 09, 00, 00, DateTimeKind.Utc);

    private readonly ReadingPaceCalculator _calculator = new();

    [Fact]
    public void Calculate_WhenNoCompletedLoans_ReturnsNull()
    {
        var result = _calculator.Calculate(new List<CompletedLoan>());

        Assert.Null(result);
    }

    [Fact]
    public void Calculate_WhenReturnedSameDay_CountsAsOneDay()
    {
        var borrowedAt = new DateTime(2026, 08, 23, 09, 00, 00, DateTimeKind.Utc);
        var returnedAt = new DateTime(2026, 08, 23, 20, 00, 00, DateTimeKind.Utc);
        var pages = 1000;

        var result = _calculator.Calculate(new List<CompletedLoan> { new CompletedLoan(1, "Duna", pages, borrowedAt, returnedAt) });

        Assert.NotNull(result);
        var item = Assert.Single(result.Breakdown);
        Assert.Equal(pages, item.PagesPerDay);
        Assert.Equal(pages, result.AveragePagesPerDay);
    }

    [Fact]
    public void Calculate_WhenSeveralLoans_WeightsTotalPagesOverTotalDays()
    {
        var result = _calculator.Calculate(new List<CompletedLoan>
        {
            new(1, "Finished in a day", 100, Anchor, Anchor.AddDays(1)),
            new(2, "Chewed on for a week and a half", 100, Anchor, Anchor.AddDays(9)),
        });

        Assert.NotNull(result);
        Assert.Equal(20.0, result.AveragePagesPerDay);
    }

    [Fact]
    public void Calculate_WhenSingleLoan_ReturnsThatLoansPace()
    {
        var result = _calculator.Calculate(new List<CompletedLoan>
        {
            new(1, "Dune", 412, Anchor, Anchor.AddDays(8)),
        });

        Assert.NotNull(result);
        Assert.Equal(51.5, result.AveragePagesPerDay);

        var only = Assert.Single(result.Breakdown);
        Assert.Equal(8.0, only.Days);
        Assert.Equal(51.5, only.PagesPerDay);
        Assert.Equal("Dune", only.Title);
    }

    [Fact]
    public void Calculate_WhenLoanLastsPartOfADay_UsesFractionalDays()
    {
        var result = _calculator.Calculate(new List<CompletedLoan>
        {
            new(1, "Foundation", 300, Anchor, Anchor.AddHours(36)),
        });

        Assert.NotNull(result);
        Assert.Equal(200.0, result.Breakdown.Single().PagesPerDay);
    }
}
