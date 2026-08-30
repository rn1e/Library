namespace Library.Service.Domain.Entities;

public class Book
{
    public long Id { get; set; }
    public string Title { get; set; } = null!;
    public string Author { get; set; } = null!;
    public int Pages { get; set; }

    public ICollection<BookCopy> Copies { get; set; } = new List<BookCopy>();
}
