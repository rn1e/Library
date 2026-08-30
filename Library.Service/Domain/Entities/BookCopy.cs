namespace Library.Service.Domain.Entities;

public class BookCopy
{
    public long Id { get; set; }
    public long BookId { get; set; }
    public Book Book { get; set; } = null!;

    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
