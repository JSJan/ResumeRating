using OpenAI;
using OpenAI.Chat;
using ResumeRating.Api.Models;
using Newtonsoft.Json;
using System.ClientModel;

namespace ResumeRating.Api.Services;

public class GitHubModelsAiService : IAiService
{
    private readonly ChatClient _chatClient;
    private readonly int _timeoutSeconds;

    public GitHubModelsAiService(IConfiguration configuration)
    {
        _timeoutSeconds = configuration.GetValue("Ai:TimeoutSeconds", 120);
        var token = configuration["GitHub:Token"]
            ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN")
            ?? throw new InvalidOperationException("GitHub:Token is not configured. Set it in appsettings.json, user-secrets, or GITHUB_TOKEN env var.");
        var model = configuration["GitHub:Model"] ?? "gpt-4o";

        var credential = new ApiKeyCredential(token);
        var options = new OpenAIClientOptions { Endpoint = new Uri("https://models.inference.ai.azure.com") };
        var client = new OpenAIClient(credential, options);
        _chatClient = client.GetChatClient(model);
    }

    public async Task<CandidateEvaluation> EvaluateResumeAsync(string resumeText, JobDescription jobDescription)
    {
        var prompt = $$"""
            You are an expert technical recruiter and hiring manager. Evaluate the following resume against the provided job description.

            ## Job Description
            **Title:** {{jobDescription.Title}}
            **Description:** {{jobDescription.Description}}
            **Required Skills:** {{jobDescription.RequiredSkills}}
            **Preferred Skills:** {{jobDescription.PreferredSkills}}
            **Experience Level:** {{jobDescription.ExperienceLevel}}

            ## Resume
            {{resumeText}}

            ## Instructions
            Rate each category from 1-10 and provide detailed feedback. Respond in this exact JSON format:
            {
                "candidateName": "<extract from resume>",
                "experienceScore": <1-10>,
                "experienceFeedback": "<detailed feedback>",
                "workHistoryScore": <1-10>,
                "workHistoryFeedback": "<detailed feedback>",
                "educationScore": <1-10>,
                "educationFeedback": "<detailed feedback>",
                "sideProjectsScore": <1-10>,
                "sideProjectsFeedback": "<detailed feedback>",
                "jobFitScore": <1-10>,
                "jobFitFeedback": "<detailed feedback>",
                "awwFactorScore": <1-10>,
                "awwFactorFeedback": "<what makes this candidate impressive>",
                "uniquenessFactor": <1-10>,
                "uniquenessFeedback": "<what sets them apart from typical candidates>",
                "overallScore": <1-10>,
                "overallFeedback": "<comprehensive summary>",
                "standout": "<key differentiators that set this candidate apart>",
                "estimatedCurrentPackage": "<estimate based on experience and skills>",
                "estimatedCurrentRole": "<likely current role/title>",
                "expectedSalaryRange": "<expected salary range for this job>",
                "recommendedForL1": <true/false>
            }

            Be thorough, honest, and constructive. Respond ONLY with the JSON object, no additional text.
            """;

        var response = await CallAsync(prompt);
        var evaluation = JsonConvert.DeserializeObject<CandidateEvaluation>(response)
            ?? throw new InvalidOperationException("Failed to parse evaluation response.");

        evaluation.Id = Guid.NewGuid().ToString();
        evaluation.EvaluatedAt = DateTime.UtcNow;
        return evaluation;
    }

    public async Task<Questionnaire> GenerateQuestionnaireAsync(CandidateEvaluation evaluation, string resumeText, JobDescription jobDescription)
    {
        var prompt = $$"""
            You are an expert technical interviewer. Generate a customized L1 interview questionnaire for this candidate.

            ## Candidate Profile
            **Name:** {{evaluation.CandidateName}}
            **Overall Score:** {{evaluation.OverallScore}}/10
            **Key Strengths:** {{evaluation.AwwFactorFeedback}}
            **Areas to Explore:** {{evaluation.OverallFeedback}}

            ## Job Description
            **Title:** {{jobDescription.Title}}
            **Required Skills:** {{jobDescription.RequiredSkills}}
            **Experience Level:** {{jobDescription.ExperienceLevel}}

            ## Resume
            {{resumeText}}

            ## Instructions
            Create 10-15 personalized interview questions that:
            1. Validate their claimed experience
            2. Probe areas of concern from the resume
            3. Test technical depth in required skills
            4. Assess cultural fit and soft skills
            5. Include behavioral/situational questions

            Respond in this exact JSON format:
            {
                "questions": [
                    {
                        "number": 1,
                        "category": "Technical|Behavioral|Situational|Role-Specific",
                        "text": "<the question>",
                        "expectedInsight": "<what you hope to learn from this answer>",
                        "difficulty": "Easy|Medium|Hard"
                    }
                ]
            }

            Respond ONLY with the JSON object.
            """;

        var response = await CallAsync(prompt);
        var parsed = JsonConvert.DeserializeObject<QuestionnaireResponse>(response)
            ?? throw new InvalidOperationException("Failed to parse questionnaire response.");

        return new Questionnaire
        {
            EvaluationId = evaluation.Id,
            CandidateName = evaluation.CandidateName,
            Questions = parsed.Questions
        };
    }

    public async Task<L1Feedback> EvaluateL1AnswersAsync(
        CandidateEvaluation evaluation,
        Questionnaire questionnaire,
        List<CandidateAnswer> answers,
        string resumeText,
        JobDescription jobDescription)
    {
        var qaSection = string.Join("\n", questionnaire.Questions.Select(q =>
        {
            var answer = answers.FirstOrDefault(a => a.QuestionNumber == q.Number);
            return $"Q{q.Number} [{q.Category}]: {q.Text}\nExpected Insight: {q.ExpectedInsight}\nCandidate Answer: {answer?.Answer ?? "No answer provided"}";
        }));

        var prompt = $$"""
            You are an expert technical interviewer evaluating L1 round answers. Provide detailed feedback for the tech hands-on round.

            ## Candidate
            **Name:** {{evaluation.CandidateName}}
            **Resume Score:** {{evaluation.OverallScore}}/10

            ## Job Description
            **Title:** {{jobDescription.Title}}
            **Required Skills:** {{jobDescription.RequiredSkills}}

            ## Questions & Answers
            {{qaSection}}

            ## Instructions
            Evaluate each answer and provide overall L1 feedback. Respond in this exact JSON format:
            {
                "answerEvaluations": [
                    {
                        "questionNumber": 1,
                        "question": "<the question>",
                        "candidateAnswer": "<their answer>",
                        "score": <1-10>,
                        "feedback": "<detailed feedback>"
                    }
                ],
                "overallL1Score": <1-10>,
                "overallL1Feedback": "<comprehensive L1 round assessment>",
                "techHandsOnRecommendation": "<specific areas and topics to test in tech hands-on round>",
                "areasToProbeInTechRound": ["<area1>", "<area2>"],
                "recommendedForTechRound": <true/false>
            }

            Be thorough and constructive. Respond ONLY with the JSON object.
            """;

        var response = await CallAsync(prompt);
        var parsed = JsonConvert.DeserializeObject<L1Feedback>(response)
            ?? throw new InvalidOperationException("Failed to parse L1 feedback response.");

        parsed.Id = Guid.NewGuid().ToString();
        parsed.EvaluationId = evaluation.Id;
        parsed.QuestionnaireId = questionnaire.Id;
        parsed.CandidateName = evaluation.CandidateName;
        parsed.EvaluatedAt = DateTime.UtcNow;
        return parsed;
    }

    public async Task<L2Questionnaire> GenerateL2QuestionnaireAsync(
        CandidateEvaluation evaluation,
        L1Feedback l1Feedback,
        string resumeText,
        JobDescription jobDescription)
    {
        var areasToProbe = string.Join(", ", l1Feedback.AreasToProbeInTechRound);

        var prompt = $$"""
            You are a senior technical architect preparing an L2 tech hands-on round for a candidate who passed L1.

            ## Candidate Profile
            **Name:** {{evaluation.CandidateName}}
            **Overall Resume Score:** {{evaluation.OverallScore}}/10
            **L1 Score:** {{l1Feedback.OverallL1Score}}/10
            **L1 Feedback:** {{l1Feedback.OverallL1Feedback}}
            **Areas to Probe:** {{areasToProbe}}
            **Tech Round Recommendation:** {{l1Feedback.TechHandsOnRecommendation}}

            ## Job Description
            **Title:** {{jobDescription.Title}}
            **Required Skills:** {{jobDescription.RequiredSkills}}
            **Experience Level:** {{jobDescription.ExperienceLevel}}

            ## Resume
            {{resumeText}}

            ## Instructions
            Generate 4 challenges tailored to this candidate:

            1. **System Design** — A real-world system design problem relevant to the job role. Should test scalability, architecture patterns, and component design.
            2. **Hands-On Coding** — A coding problem that tests practical implementation skills in their claimed tech stack. Include clear input/output expectations.
            3. **Design Thinking** — A product/UX design thinking scenario that tests user empathy, problem framing, and solution ideation.
            4. **Trade-Off Analysis** — A scenario requiring the candidate to analyze and justify technology/architecture trade-offs (e.g., SQL vs NoSQL, monolith vs microservices, consistency vs availability).

            Respond in this exact JSON format:
            {
                "systemDesign": {
                    "scenario": "<detailed problem statement>",
                    "expectedApproach": "<what a strong answer looks like>",
                    "evaluationCriteria": ["<criterion1>", "<criterion2>", "<criterion3>"]
                },
                "handsOnCoding": {
                    "scenario": "<detailed coding problem>",
                    "expectedApproach": "<expected solution approach>",
                    "evaluationCriteria": ["<criterion1>", "<criterion2>", "<criterion3>"]
                },
                "designThinking": {
                    "scenario": "<design thinking scenario>",
                    "expectedApproach": "<expected approach>",
                    "evaluationCriteria": ["<criterion1>", "<criterion2>", "<criterion3>"]
                },
                "tradeOffAnalysis": {
                    "scenario": "<trade-off scenario>",
                    "expectedApproach": "<expected analysis>",
                    "evaluationCriteria": ["<criterion1>", "<criterion2>", "<criterion3>"]
                }
            }

            Make challenges specific to the candidate's experience and the job requirements. Respond ONLY with the JSON object.
            """;

        var response = await CallAsync(prompt);
        var parsed = JsonConvert.DeserializeObject<L2QuestionnaireResponse>(response)
            ?? throw new InvalidOperationException("Failed to parse L2 questionnaire response.");

        return new L2Questionnaire
        {
            EvaluationId = evaluation.Id,
            L1FeedbackId = l1Feedback.Id,
            CandidateName = evaluation.CandidateName,
            SystemDesign = parsed.SystemDesign,
            HandsOnCoding = parsed.HandsOnCoding,
            DesignThinking = parsed.DesignThinking,
            TradeOffAnalysis = parsed.TradeOffAnalysis
        };
    }

    public async Task<L2Assessment> EvaluateL2AnswersAsync(
        CandidateEvaluation evaluation,
        L2Questionnaire questionnaire,
        L2AnswersRequest answers,
        string resumeText,
        JobDescription jobDescription)
    {
        var prompt = $$"""
            You are a senior technical architect evaluating L2 tech hands-on round answers. Provide thorough, honest assessment.

            ## Candidate
            **Name:** {{evaluation.CandidateName}}
            **Resume Score:** {{evaluation.OverallScore}}/10

            ## Job Description
            **Title:** {{jobDescription.Title}}
            **Required Skills:** {{jobDescription.RequiredSkills}}

            ## Challenge 1: System Design
            **Scenario:** {{questionnaire.SystemDesign.Scenario}}
            **Expected Approach:** {{questionnaire.SystemDesign.ExpectedApproach}}
            **Candidate's Answer:** {{answers.SystemDesignAnswer}}

            ## Challenge 2: Hands-On Coding
            **Scenario:** {{questionnaire.HandsOnCoding.Scenario}}
            **Expected Approach:** {{questionnaire.HandsOnCoding.ExpectedApproach}}
            **Candidate's Answer:** {{answers.HandsOnCodingAnswer}}

            ## Challenge 3: Design Thinking
            **Scenario:** {{questionnaire.DesignThinking.Scenario}}
            **Expected Approach:** {{questionnaire.DesignThinking.ExpectedApproach}}
            **Candidate's Answer:** {{answers.DesignThinkingAnswer}}

            ## Challenge 4: Trade-Off Analysis
            **Scenario:** {{questionnaire.TradeOffAnalysis.Scenario}}
            **Expected Approach:** {{questionnaire.TradeOffAnalysis.ExpectedApproach}}
            **Candidate's Answer:** {{answers.TradeOffAnalysisAnswer}}

            ## Instructions
            Evaluate each challenge response and provide a final hiring recommendation. Use this scale:
            - **Strong Hire** (8-10): Exceptional across all areas
            - **Hire** (6-7): Good with minor gaps
            - **Lean No Hire** (4-5): Significant concerns
            - **No Hire** (1-3): Does not meet requirements

            Respond in this exact JSON format:
            {
                "systemDesign": {
                    "score": <1-10>,
                    "feedback": "<detailed feedback>"
                },
                "handsOnCoding": {
                    "score": <1-10>,
                    "feedback": "<detailed feedback>"
                },
                "designThinking": {
                    "score": <1-10>,
                    "feedback": "<detailed feedback>"
                },
                "tradeOffAnalysis": {
                    "score": <1-10>,
                    "feedback": "<detailed feedback>"
                },
                "overallL2Score": <1-10>,
                "overallL2Feedback": "<comprehensive assessment>",
                "strengths": "<key strengths demonstrated>",
                "weaknesses": "<areas of concern>",
                "hiringRecommendation": "Strong Hire|Hire|Lean No Hire|No Hire",
                "recommendedForHire": <true/false>
            }

            Be thorough and constructive. Respond ONLY with the JSON object.
            """;

        var response = await CallAsync(prompt);
        var parsed = JsonConvert.DeserializeObject<L2AssessmentResponse>(response)
            ?? throw new InvalidOperationException("Failed to parse L2 assessment response.");

        return new L2Assessment
        {
            EvaluationId = evaluation.Id,
            L1FeedbackId = questionnaire.L1FeedbackId,
            CandidateName = evaluation.CandidateName,
            SystemDesign = new L2Challenge
            {
                Scenario = questionnaire.SystemDesign.Scenario,
                ExpectedApproach = questionnaire.SystemDesign.ExpectedApproach,
                EvaluationCriteria = questionnaire.SystemDesign.EvaluationCriteria,
                CandidateResponse = answers.SystemDesignAnswer,
                Score = parsed.SystemDesign.Score,
                Feedback = parsed.SystemDesign.Feedback
            },
            HandsOnCoding = new L2Challenge
            {
                Scenario = questionnaire.HandsOnCoding.Scenario,
                ExpectedApproach = questionnaire.HandsOnCoding.ExpectedApproach,
                EvaluationCriteria = questionnaire.HandsOnCoding.EvaluationCriteria,
                CandidateResponse = answers.HandsOnCodingAnswer,
                Score = parsed.HandsOnCoding.Score,
                Feedback = parsed.HandsOnCoding.Feedback
            },
            DesignThinking = new L2Challenge
            {
                Scenario = questionnaire.DesignThinking.Scenario,
                ExpectedApproach = questionnaire.DesignThinking.ExpectedApproach,
                EvaluationCriteria = questionnaire.DesignThinking.EvaluationCriteria,
                CandidateResponse = answers.DesignThinkingAnswer,
                Score = parsed.DesignThinking.Score,
                Feedback = parsed.DesignThinking.Feedback
            },
            TradeOffAnalysis = new L2Challenge
            {
                Scenario = questionnaire.TradeOffAnalysis.Scenario,
                ExpectedApproach = questionnaire.TradeOffAnalysis.ExpectedApproach,
                EvaluationCriteria = questionnaire.TradeOffAnalysis.EvaluationCriteria,
                CandidateResponse = answers.TradeOffAnalysisAnswer,
                Score = parsed.TradeOffAnalysis.Score,
                Feedback = parsed.TradeOffAnalysis.Feedback
            },
            OverallL2Score = parsed.OverallL2Score,
            OverallL2Feedback = parsed.OverallL2Feedback,
            Strengths = parsed.Strengths,
            Weaknesses = parsed.Weaknesses,
            HiringRecommendation = parsed.HiringRecommendation,
            RecommendedForHire = parsed.RecommendedForHire
        };
    }

    private async Task<string> CallAsync(string prompt)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("You are a precise JSON-outputting assistant. Always respond with valid JSON only."),
            new UserChatMessage(prompt)
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
        var completion = await _chatClient.CompleteChatAsync(messages, cancellationToken: cts.Token);
        var content = completion.Value.Content[0].Text;

        // Strip markdown code fences if present
        if (content.StartsWith("```"))
        {
            var firstNewline = content.IndexOf('\n');
            var lastFence = content.LastIndexOf("```");
            if (firstNewline >= 0 && lastFence > firstNewline)
            {
                content = content[(firstNewline + 1)..lastFence].Trim();
            }
        }

        return content;
    }

    private class QuestionnaireResponse
    {
        public List<Question> Questions { get; set; } = [];
    }

    private class L2QuestionnaireResponse
    {
        public L2ChallengePrompt SystemDesign { get; set; } = new();
        public L2ChallengePrompt HandsOnCoding { get; set; } = new();
        public L2ChallengePrompt DesignThinking { get; set; } = new();
        public L2ChallengePrompt TradeOffAnalysis { get; set; } = new();
    }

    private class L2ChallengeScoreResponse
    {
        public int Score { get; set; }
        public string Feedback { get; set; } = string.Empty;
    }

    private class L2AssessmentResponse
    {
        public L2ChallengeScoreResponse SystemDesign { get; set; } = new();
        public L2ChallengeScoreResponse HandsOnCoding { get; set; } = new();
        public L2ChallengeScoreResponse DesignThinking { get; set; } = new();
        public L2ChallengeScoreResponse TradeOffAnalysis { get; set; } = new();
        public int OverallL2Score { get; set; }
        public string OverallL2Feedback { get; set; } = string.Empty;
        public string Strengths { get; set; } = string.Empty;
        public string Weaknesses { get; set; } = string.Empty;
        public string HiringRecommendation { get; set; } = string.Empty;
        public bool RecommendedForHire { get; set; }
    }
}
