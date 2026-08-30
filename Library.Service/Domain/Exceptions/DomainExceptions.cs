namespace Library.Service.Domain.Exceptions;

public abstract class NotFoundException : Exception
{
    protected NotFoundException(string message) : base(message) { }
}

public sealed class BorrowerNotFoundException : NotFoundException
{
    public BorrowerNotFoundException(long borrowerId) : base($"Borrower {borrowerId} was not found") { }
}

public sealed class BookNotFoundException : NotFoundException
{
    public BookNotFoundException(long bookId) : base($"Book {bookId} was not found") { }
}

public sealed class LoanNotFoundException : NotFoundException
{
    public LoanNotFoundException(long loanId) : base($"Loan {loanId} was not found") { }
}

public sealed class NoCopiesAvailableException : Exception
{
    public NoCopiesAvailableException(long bookId)
        : base($"Every copy of book {bookId} is currently on loan") { }
}
