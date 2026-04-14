namespace ResumeRating.Api.Models;

public class JobDescription
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RequiredSkills { get; set; } = string.Empty;
    public string PreferredSkills { get; set; } = string.Empty;
    public string ExperienceLevel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
