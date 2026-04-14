namespace ResumeRating.Api.Models;

public class Questionnaire
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EvaluationId { get; set; } = string.Empty;
    public string CandidateName { get; set; } = string.Empty;
    public List<Question> Questions { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Question
{
    public int Number { get; set; }
    public string Category { get; set; } = string.Empty; // Technical, Behavioral, Situational, Role-Specific
    public string Text { get; set; } = string.Empty;
    public string ExpectedInsight { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty; // Easy, Medium, Hard
}
