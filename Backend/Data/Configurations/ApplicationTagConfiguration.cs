using Backend.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Data.Configurations;

public class ApplicationTagConfiguration : IEntityTypeConfiguration<ApplicationTag>
{
    public void Configure(EntityTypeBuilder<ApplicationTag> builder)
    {
        builder.HasKey(a => new { a.JobApplicationId, a.TagId });

        builder.HasOne(a => a.JobApplication)
            .WithMany(j => j.ApplicationTags)
            .HasForeignKey(a => a.JobApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Tag)
            .WithMany(t => t.ApplicationTags)
            .HasForeignKey(a => a.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
