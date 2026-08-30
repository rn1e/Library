namespace Library.Service.Domain.Entities;

public class Borrower
{
    public long Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;

    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
