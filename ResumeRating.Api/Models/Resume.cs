namespace ResumeRating.Api.Models;

public class Resume
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = string.Empty;
    public string CandidateName { get; set; } = string.Empty;
    public string ExtractedText { get; set; } = string.Empty;
    public string LinkedInUrl { get; set; } = string.Empty;
    public string GitHubUsername { get; set; } = string.Empty;
    public string GitHubProfileSummary { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
