namespace Backend.Domain;

public class ApplicationTag
{
    public Guid JobApplicationId { get; set; }

    public Guid TagId { get; set; }

    public JobApplication JobApplication { get; set; } = null!;

    public Tag Tag { get; set; } = null!;
}
