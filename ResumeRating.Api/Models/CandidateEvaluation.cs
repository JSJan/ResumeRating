namespace ResumeRating.Api.Models;

public class CandidateEvaluation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ResumeId { get; set; } = string.Empty;
    public string JobDescriptionId { get; set; } = string.Empty;
    public string CandidateName { get; set; } = string.Empty;

    // Rating scores (1-10)
    public int ExperienceScore { get; set; }
    public int WorkHistoryScore { get; set; }
    public int EducationScore { get; set; }
    public int SideProjectsScore { get; set; }
    public int JobFitScore { get; set; }
    public int AwwFactorScore { get; set; }
    public int UniquenessFactor { get; set; }
    public int OverallScore { get; set; }

    // Detailed feedback
    public string ExperienceFeedback { get; set; } = string.Empty;
    public string WorkHistoryFeedback { get; set; } = string.Empty;
    public string EducationFeedback { get; set; } = string.Empty;
    public string SideProjectsFeedback { get; set; } = string.Empty;
    public string JobFitFeedback { get; set; } = string.Empty;
    public string AwwFactorFeedback { get; set; } = string.Empty;
    public string UniquenessFeedback { get; set; } = string.Empty;
    public string OverallFeedback { get; set; } = string.Empty;

    // What sets them apart
    public string Standout { get; set; } = string.Empty;

    // Salary estimates
    public string EstimatedCurrentPackage { get; set; } = string.Empty;
    public string EstimatedCurrentRole { get; set; } = string.Empty;
    public string ExpectedSalaryRange { get; set; } = string.Empty;

    public bool RecommendedForL1 { get; set; }
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
}
