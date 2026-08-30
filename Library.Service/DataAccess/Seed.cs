using Library.Service.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Library.Service.DataAccess;

public static class Seed
{
    public static readonly DateTime Anchor = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime WindowFrom = Anchor;
    public static readonly DateTime WindowTo = Anchor.AddDays(30);

    public static async Task ApplyAsync(LibraryDbContext db, CancellationToken ct = default)
    {
        if (await db.Books.AnyAsync(ct))
            return;

        var dune = NewBook("Dune", "Frank Herbert", 412, copies: 3);
        var neuromancer = NewBook("Neuromancer", "William Gibson", 271, copies: 2);
        var snowCrash = NewBook("Snow Crash", "Neal Stephenson", 440, copies: 2);
        var dispossessed = NewBook("The Dispossessed", "Ursula K. Le Guin", 341, copies: 2);
        var foundation = NewBook("Foundation", "Isaac Asimov", 255, copies: 2);
        var leftHand = NewBook("The Left Hand of Darkness", "Ursula K. Le Guin", 304, copies: 2);
        var hyperion = NewBook("Hyperion", "Dan Simmons", 482, copies: 1);
        var solaris = NewBook("Solaris", "Stanislaw Lem", 204, copies: 1);
        var blindsight = NewBook("Blindsight", "Peter Watts", 384, copies: 1);
        var fireUponTheDeep = NewBook("A Fire Upon the Deep", "Vernor Vinge", 613, copies: 1);

        var alice = NewBorrower("Alice", "Nguyen");
        var bob = NewBorrower("Bob", "Marsden");
        var clara = NewBorrower("Clara", "Okafor");
        var daniel = NewBorrower("Daniel", "Weiss");
        var eve = NewBorrower("Eve", "Lindqvist");
        var frank = NewBorrower("Frank", "Adeyemi");

        var loans = new[]
        {
            NewLoan(bob, dune, 0, -20, -10),
            NewLoan(clara, dispossessed, 0, -18, -12),
            NewLoan(bob, snowCrash, 0, -15, -5),
            NewLoan(bob, neuromancer, 1, -8, -3),
            NewLoan(clara, foundation, 1, -25, -20),

            NewLoan(alice, dune, 0, 0, 7),
            NewLoan(alice, solaris, 0, 8, 8.5),
            NewLoan(alice, hyperion, 0, 20, null),

            NewLoan(bob, dune, 1, 1, 5),
            NewLoan(bob, neuromancer, 0, 6, 12),
            NewLoan(bob, snowCrash, 0, 13, 20),
            NewLoan(bob, foundation, 0, 21, 25),

            NewLoan(clara, dune, 2, 2, 9),
            NewLoan(clara, neuromancer, 0, 14, 18),
            NewLoan(clara, dispossessed, 0, 15, 22),

            NewLoan(daniel, dune, 0, 10, 14),
            NewLoan(daniel, snowCrash, 1, 15, 19),
            NewLoan(daniel, neuromancer, 0, 20, 26),

            NewLoan(eve, blindsight, 0, 5, null),
        };

        db.AddRange(dune, neuromancer, snowCrash, dispossessed, foundation,
                    leftHand, hyperion, solaris, blindsight, fireUponTheDeep);
        db.AddRange(alice, bob, clara, daniel, eve, frank);
        db.AddRange(loans);

        await db.SaveChangesAsync(ct);
    }

    private static Book NewBook(string title, string author, int pages, int copies) => new()
    {
        Title = title,
        Author = author,
        Pages = pages,
        Copies = Enumerable.Range(0, copies).Select(_ => new BookCopy()).ToList(),
    };

    private static Borrower NewBorrower(string firstName, string lastName) => new()
    {
        FirstName = firstName,
        LastName = lastName,
    };

    private static Loan NewLoan(Borrower borrower, Book book, int copyIndex, double borrowedOn, double? returnedOn) => new()
    {
        Borrower = borrower,
        BookCopy = book.Copies.ElementAt(copyIndex),
        BorrowedAt = Anchor.AddDays(borrowedOn),
        ReturnedAt = returnedOn is { } r ? Anchor.AddDays(r) : null,
    };
}
