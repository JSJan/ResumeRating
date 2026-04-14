using OpenAI;
using OpenAI.Chat;
using ResumeRating.Api.Models;
using Newtonsoft.Json;
using System.ClientModel;

namespace ResumeRating.Api.Services;

public class GitHubModelsAiService : IAiService
{
    private readonly ChatClient _chatClient;

    public GitHubModelsAiService(IConfiguration configuration)
    {
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

    private async Task<string> CallAsync(string prompt)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("You are a precise JSON-outputting assistant. Always respond with valid JSON only."),
            new UserChatMessage(prompt)
        };

        var completion = await _chatClient.CompleteChatAsync(messages);
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
}
