using Library.Service.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Service.DataAccess.Configurations;

public class BorrowerConfiguration : IEntityTypeConfiguration<Borrower>
{
    public void Configure(EntityTypeBuilder<Borrower> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(b => b.LastName).HasMaxLength(100).IsRequired();
    }
}
