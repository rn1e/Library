namespace Library.Tests.Unit.WarmUp;

public class WarmUpTasksTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(1024)]
    [InlineData(4_611_686_018_427_387_904)]   // 2^62, to prove this is not int arithmetic
    public void IsPowerOfTwo_WhenIdIsAPowerOfTwo_ReturnsTrue(long bookId)
    {
        Assert.True(WarmUpTasks.IsPowerOfTwo(bookId));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(1023)]
    public void IsPowerOfTwo_WhenIdIsNotAPowerOfTwo_ReturnsFalse(long bookId)
    {
        Assert.False(WarmUpTasks.IsPowerOfTwo(bookId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    [InlineData(long.MinValue)]
    public void IsPowerOfTwo_WhenIdIsZeroOrNegative_ReturnsFalse(long bookId)
    {
        Assert.False(WarmUpTasks.IsPowerOfTwo(bookId));
    }

    [Fact]
    public void Reverse_WhenTitleIsPlainText_ReversesIt()
    {
        Assert.Equal("enuD", WarmUpTasks.Reverse("Dune"));
    }

    [Fact]
    public void Reverse_WhenTitleIsEmpty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, WarmUpTasks.Reverse(string.Empty));
    }

    [Fact]
    public void Repeat_WhenTimesIsPositive_ConcatenatesThatMany()
    {
        Assert.Equal("DuneDuneDune", WarmUpTasks.Repeat("Dune", 3));
    }

    [Fact]
    public void Repeat_WhenTimesIsZero_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, WarmUpTasks.Repeat("Dune", 0));
    }

    [Fact]
    public void Repeat_WhenTimesIsNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WarmUpTasks.Repeat("Dune", -1));
    }

    [Fact]
    public void OddNumbers_WhenMaxIsOneHundred_ReturnsFiftyNumbers()
    {
        var odds = WarmUpTasks.OddNumbers(100).ToList();

        Assert.Equal(50, odds.Count);
        Assert.Equal(1, odds.First());
        Assert.Equal(99, odds.Last());
        Assert.All(odds, n => Assert.Equal(1, Math.Abs(n % 2)));
    }

    [Fact]
    public void OddNumbers_WhenMaxIsZero_ReturnsNothing()
    {
        Assert.Empty(WarmUpTasks.OddNumbers(0));
    }
}
