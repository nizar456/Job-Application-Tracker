using Backend.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Data.Configurations;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(200);

        builder.Property(c => c.Email)
            .HasMaxLength(256);

        builder.Property(c => c.Phone)
            .HasMaxLength(50);

        builder.Property(c => c.Role)
            .HasMaxLength(200);

        builder.HasIndex(c => c.JobApplicationId);

        builder.HasOne(c => c.JobApplication)
            .WithMany(j => j.Contacts)
            .HasForeignKey(c => c.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
