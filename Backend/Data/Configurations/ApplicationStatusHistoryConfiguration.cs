using Backend.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Data.Configurations;

public class ApplicationStatusHistoryConfiguration : IEntityTypeConfiguration<ApplicationStatusHistory>
{
    public void Configure(EntityTypeBuilder<ApplicationStatusHistory> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Status)
            .HasConversion<int>();

        builder.Property(h => h.Note)
            .HasMaxLength(1000);

        builder.HasIndex(h => new { h.JobApplicationId, h.ChangedAtUtc });

        builder.HasOne(h => h.JobApplication)
            .WithMany(j => j.StatusHistory)
            .HasForeignKey(h => h.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
