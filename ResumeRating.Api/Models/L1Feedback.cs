namespace ResumeRating.Api.Models;

public class L1Feedback
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EvaluationId { get; set; } = string.Empty;
    public string QuestionnaireId { get; set; } = string.Empty;
    public string CandidateName { get; set; } = string.Empty;
    public List<AnswerEvaluation> AnswerEvaluations { get; set; } = [];
    public int OverallL1Score { get; set; }
    public string OverallL1Feedback { get; set; } = string.Empty;
    public string TechHandsOnRecommendation { get; set; } = string.Empty;
    public List<string> AreasToProbeInTechRound { get; set; } = [];
    public bool RecommendedForTechRound { get; set; }
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
}

public class AnswerEvaluation
{
    public int QuestionNumber { get; set; }
    public string Question { get; set; } = string.Empty;
    public string CandidateAnswer { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Feedback { get; set; } = string.Empty;
}

public class L1AnswersRequest
{
    public string EvaluationId { get; set; } = string.Empty;
    public string QuestionnaireId { get; set; } = string.Empty;
    public List<CandidateAnswer> Answers { get; set; } = [];
}

public class CandidateAnswer
{
    public int QuestionNumber { get; set; }
    public string Answer { get; set; } = string.Empty;
}
