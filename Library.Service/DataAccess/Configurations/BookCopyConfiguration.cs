using Library.Service.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Service.DataAccess.Configurations;

public class BookCopyConfiguration : IEntityTypeConfiguration<BookCopy>
{
    public void Configure(EntityTypeBuilder<BookCopy> builder)
    {
        builder.HasKey(c => c.Id);

        builder.HasOne(c => c.Book)
               .WithMany(b => b.Copies)
               .HasForeignKey(c => c.BookId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.BookId);
    }
}
