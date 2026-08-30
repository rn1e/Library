namespace Library.Service.Domain.Entities;

public class Loan
{
    public long Id { get; set; }
    public long BookCopyId { get; set; }
    public BookCopy BookCopy { get; set; } = null!;

    public long BorrowerId { get; set; }
    public Borrower Borrower { get; set; } = null!;

    public DateTime BorrowedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
}