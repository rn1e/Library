using Library.Service.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Library.Service.DataAccess.Configurations;


public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    private static readonly ValueConverter<DateTime, DateTime> UtcConverter =
        new(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> NullableUtcConverter =
        new(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.BorrowedAt).HasConversion(UtcConverter);
        builder.Property(l => l.ReturnedAt).HasConversion(NullableUtcConverter);

        builder.HasOne(l => l.BookCopy)
               .WithMany(c => c.Loans)
               .HasForeignKey(l => l.BookCopyId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Borrower)
               .WithMany(b => b.Loans)
               .HasForeignKey(l => l.BorrowerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.BorrowerId);
        builder.HasIndex(l => l.BorrowedAt);

        builder.HasIndex(l => l.BookCopyId)
               .HasFilter("[ReturnedAt] IS NULL")
               .IsUnique()
               .HasDatabaseName("IX_Loan_ActiveCopy");
    }
}