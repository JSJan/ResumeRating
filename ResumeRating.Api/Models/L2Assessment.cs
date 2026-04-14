namespace ResumeRating.Api.Models;

public class L2Assessment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EvaluationId { get; set; } = string.Empty;
    public string L1FeedbackId { get; set; } = string.Empty;
    public string CandidateName { get; set; } = string.Empty;

    // System Design
    public L2Challenge SystemDesign { get; set; } = new();

    // Hands-On Coding
    public L2Challenge HandsOnCoding { get; set; } = new();

    // Design Thinking
    public L2Challenge DesignThinking { get; set; } = new();

    // Trade-Off Analysis
    public L2Challenge TradeOffAnalysis { get; set; } = new();

    // Overall
    public int OverallL2Score { get; set; }
    public string OverallL2Feedback { get; set; } = string.Empty;
    public string Strengths { get; set; } = string.Empty;
    public string Weaknesses { get; set; } = string.Empty;
    public string HiringRecommendation { get; set; } = string.Empty; // Strong Hire / Hire / Lean No Hire / No Hire
    public bool RecommendedForHire { get; set; }
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
}

public class L2Challenge
{
    public string Scenario { get; set; } = string.Empty;
    public string ExpectedApproach { get; set; } = string.Empty;
    public List<string> EvaluationCriteria { get; set; } = [];
    public string CandidateResponse { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Feedback { get; set; } = string.Empty;
}

public class L2Questionnaire
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EvaluationId { get; set; } = string.Empty;
    public string L1FeedbackId { get; set; } = string.Empty;
    public string CandidateName { get; set; } = string.Empty;

    public L2ChallengePrompt SystemDesign { get; set; } = new();
    public L2ChallengePrompt HandsOnCoding { get; set; } = new();
    public L2ChallengePrompt DesignThinking { get; set; } = new();
    public L2ChallengePrompt TradeOffAnalysis { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class L2ChallengePrompt
{
    public string Scenario { get; set; } = string.Empty;
    public string ExpectedApproach { get; set; } = string.Empty;
    public List<string> EvaluationCriteria { get; set; } = [];
}

public class L2AnswersRequest
{
    public string EvaluationId { get; set; } = string.Empty;
    public string L2QuestionnaireId { get; set; } = string.Empty;
    public string SystemDesignAnswer { get; set; } = string.Empty;
    public string HandsOnCodingAnswer { get; set; } = string.Empty;
    public string DesignThinkingAnswer { get; set; } = string.Empty;
    public string TradeOffAnalysisAnswer { get; set; } = string.Empty;
}
