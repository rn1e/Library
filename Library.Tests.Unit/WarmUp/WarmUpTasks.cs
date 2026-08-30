using System.Text;

namespace Library.Tests.Unit.WarmUp;

public static class WarmUpTasks
{
    public static bool IsPowerOfTwo(long bookId) => bookId > 0 && (bookId & (bookId - 1)) == 0;

    public static string Reverse(string title)
    {
        ArgumentNullException.ThrowIfNull(title);

        return new string(title.Reverse().ToArray());
    }

    public static string Repeat(string title, int times)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentOutOfRangeException.ThrowIfNegative(times);

        var builder = new StringBuilder(title.Length * times);

        for (var i = 0; i < times; i++)
            builder.Append(title);

        return builder.ToString();
    }

    public static IEnumerable<int> OddNumbers(int max)
    {
        for (var i = 1; i <= max; i += 2)
            yield return i;
    }
}
