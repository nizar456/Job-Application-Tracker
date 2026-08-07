using Backend.Domain;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Data.Configurations;

public class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.RoleTitle)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(j => j.Location)
            .HasMaxLength(200);

        builder.Property(j => j.WorkMode)
            .HasConversion<int>();

        builder.Property(j => j.JobUrl)
            .HasMaxLength(500);

        builder.Property(j => j.Source)
            .HasMaxLength(200);

        builder.Property(j => j.Status)
            .HasConversion<int>();

        builder.HasIndex(j => j.UserId);
        builder.HasIndex(j => j.CompanyId);
        builder.HasIndex(j => j.ResumeVersionId);
        builder.HasIndex(j => j.Status);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(j => j.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(j => j.Company)
            .WithMany(c => c.JobApplications)
            .HasForeignKey(j => j.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(j => j.ResumeVersion)
            .WithMany(r => r.JobApplications)
            .HasForeignKey(j => j.ResumeVersionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
