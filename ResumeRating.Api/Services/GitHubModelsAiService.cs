using OpenAI;
using OpenAI.Chat;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using ResumeRating.Api.Models;
using Newtonsoft.Json;
using System.ClientModel;

namespace ResumeRating.Api.Services;

public class GitHubModelsAiService : IAiService
{
    private readonly ChatClient _evalClient;
    private readonly ChatClient _lightClient;
    private readonly AnthropicClient? _anthropicClient;
    private readonly string? _anthropicLightModel;
    private readonly bool _useAnthropicForLight;
    private readonly int _timeoutSeconds;
    private readonly string _evalModel;
    private readonly string _lightModel;
    private readonly List<TokenUsageEntry> _usageLog = [];
    private readonly object _logLock = new();

    // Pricing per 1M tokens
    private static readonly Dictionary<string, (double Input, double Output)> ModelPricing = new()
    {
        ["gpt-4o"] = (2.50, 10.00),
        ["gpt-4o-mini"] = (0.15, 0.60),
        ["claude-3-5-haiku-20241022"] = (0.25, 1.25),
    };

    public GitHubModelsAiService(IConfiguration configuration)
    {
        _timeoutSeconds = configuration.GetValue("Ai:TimeoutSeconds", 120);
        var token = configuration["GitHub:Token"]
            ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN")
            ?? throw new InvalidOperationException("GitHub:Token is not configured. Set it in appsettings.json, user-secrets, or GITHUB_TOKEN env var.");
        _evalModel = configuration["GitHub:Model"] ?? "gpt-4o";
        _lightModel = configuration["GitHub:LightModel"] ?? "gpt-4o-mini";

        var credential = new ApiKeyCredential(token);
        var options = new OpenAIClientOptions { Endpoint = new Uri("https://models.inference.ai.azure.com") };
        var client = new OpenAIClient(credential, options);
        _evalClient = client.GetChatClient(_evalModel);
        _lightClient = client.GetChatClient(_lightModel);

        // Optional Anthropic Haiku for light calls
        var anthropicKey = configuration["Anthropic:ApiKey"];
        _anthropicLightModel = configuration["Anthropic:LightModel"];
        if (!string.IsNullOrEmpty(anthropicKey) && !string.IsNullOrEmpty(_anthropicLightModel))
        {
            _anthropicClient = new AnthropicClient(new APIAuthentication(anthropicKey));
            _useAnthropicForLight = true;
        }
    }

    public async Task<CandidateEvaluation> EvaluateResumeAsync(string resumeText, JobDescription jobDescription, string? linkedInUrl = null, string? gitHubSummary = null, string? codeAnalysis = null, ResumeAnalysis? preAnalysis = null, GitHubSkillMatch? gitHubSkillMatch = null)
    {
        // Truncate resume text to ~8000 chars (~2000 tokens) to reduce cost
        var truncatedResume = resumeText.Length > 8000
            ? resumeText[..8000] + "\n... [resume truncated for brevity]"
            : resumeText;

        // Build conditional sections — only include if data exists
        var optionalSections = new System.Text.StringBuilder();

        if (!string.IsNullOrWhiteSpace(linkedInUrl))
            optionalSections.AppendLine($"\n## LinkedIn (authenticity cross-reference only)\n**URL:** {linkedInUrl}\nUse ONLY to verify career timeline consistency. Do NOT weight profile quality.");

        if (!string.IsNullOrWhiteSpace(gitHubSummary))
            optionalSections.AppendLine($"\n## GitHub Profile (BONUS — increase scores if impressive)\n{gitHubSummary}");

        if (!string.IsNullOrWhiteSpace(codeAnalysis))
            optionalSections.AppendLine($"\n## Source Code from GitHub Repos\nAnalyze for code quality, patterns, naming, error handling, innovation, and true proficiency.\n\n{codeAnalysis}");

        // Build pre-computed hints section from local analysis
        var hintsSection = "";
        if (preAnalysis != null)
        {
            var hints = new System.Text.StringBuilder();
            hints.AppendLine("\n## Pre-Computed Analysis (verified data — use as scoring hints)");
            hints.AppendLine($"- **Skill match:** {preAnalysis.KeywordMatchPercent:F0}% of JD skills found in resume");
            hints.AppendLine($"- **Matched skills:** {string.Join(", ", preAnalysis.MatchedSkills)}");
            hints.AppendLine($"- **Missing skills:** {string.Join(", ", preAnalysis.MissingSkills)}");
            hints.AppendLine($"- **Est. years of experience:** {preAnalysis.EstimatedYearsOfExperience}");
            hints.AppendLine($"- **Education level:** {preAnalysis.EducationLevel}");
            hints.AppendLine($"- **Readability score:** {preAnalysis.ReadabilityScore:F1} (Flesch-Kincaid; low = complex, high = simple)");
            hints.AppendLine($"- **Sentence length variance:** {preAnalysis.SentenceLengthVariance:F1} (low variance may indicate AI-generated text)");
            hints.AppendLine($"- **AI-generated probability:** {preAnalysis.AiGeneratedProbability:P0}");
            if (preAnalysis.PowerVerbsFound.Count > 0)
                hints.AppendLine($"- **Power verbs found:** {string.Join(", ", preAnalysis.PowerVerbsFound)}");
            if (preAnalysis.BuzzwordCounts.Count > 0)
                hints.AppendLine($"- **Repeated JD keywords (3+):** {string.Join(", ", preAnalysis.BuzzwordCounts.Select(kv => $"{kv.Key} ({kv.Value}×)"))}");

            if (gitHubSkillMatch != null)
            {
                hints.AppendLine($"- **GitHub skill overlap:** {gitHubSkillMatch.OverlapPercent:F0}%");
                if (gitHubSkillMatch.MatchedLanguages.Count > 0)
                    hints.AppendLine($"- **GitHub matched languages:** {string.Join(", ", gitHubSkillMatch.MatchedLanguages)}");
            }

            hintsSection = hints.ToString();
        }

        var prompt = $$"""
            You are an expert technical recruiter, hiring manager, and senior code reviewer. Evaluate the resume against the job description.

            ## Job Description
            **Title:** {{jobDescription.Title}}
            **Description:** {{jobDescription.Description}}
            **Required Skills:** {{jobDescription.RequiredSkills}}
            **Preferred Skills:** {{jobDescription.PreferredSkills}}
            **Experience Level:** {{jobDescription.ExperienceLevel}}

            ## Resume
            {{truncatedResume}}
            {{optionalSections}}
            {{hintsSection}}

            ## Resume Quality Assessment
            Assess authenticity, buzzword stuffing, and AI-generated content in a single analysis:
            - Check keyword density, specificity vs. vagueness, consistency with GitHub code, temporal plausibility
            - Detect buzzword repetition (3+ times without different context), copy-paste from JD, AI writing patterns (uniform sentence structure, power verb overuse, generic phrasing)
            - Positive signals: specific project names, exact metrics, named tools, personal voice

            ## Scoring Rules
            - **LinkedIn**: LOW weight, authenticity cross-reference only. Default 5/10 if not provided.
            - **GitHub**: BONUS only. Boost scores if strong code exists. Default 5/10 if not provided.
            - **Resume + JD fit**: PRIMARY criteria. Low authenticity/buzzword/AI scores should REDUCE overallScore.

            Rate 1-10 per category. Respond with ONLY this JSON:
            {"candidateName":"","experienceScore":0,"experienceFeedback":"","workHistoryScore":0,"workHistoryFeedback":"","educationScore":0,"educationFeedback":"","sideProjectsScore":0,"sideProjectsFeedback":"","jobFitScore":0,"jobFitFeedback":"","awwFactorScore":0,"awwFactorFeedback":"","uniquenessFactor":0,"uniquenessFeedback":"","gitHubScore":0,"gitHubFeedback":"","onlinePresenceScore":0,"onlinePresenceFeedback":"","codeProficiencyScore":0,"codeProficiencyFeedback":"","resumeAuthenticityScore":0,"resumeAuthenticityFeedback":"","buzzwordScore":0,"buzzwordFeedback":"","aiGeneratedScore":0,"aiGeneratedFeedback":"","tailoringRedFlags":[],"buzzwordsDetected":[],"authenticityAnalysis":"","overallScore":0,"overallFeedback":"","standout":"","estimatedCurrentPackage":"","estimatedCurrentRole":"","expectedSalaryRange":"","recommendedForL1":false}

            Fill every field with detailed content. Be thorough, honest, and constructive.
            """;

        var response = await CallAsync(prompt, "evaluate-resume");
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

        var response = await CallLightAsync(prompt, "generate-l1-questionnaire");
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

        var response = await CallLightAsync(prompt, "evaluate-l1-answers");
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

        var response = await CallLightAsync(prompt, "generate-l2-questionnaire");
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

        var response = await CallLightAsync(prompt, "evaluate-l2-answers");
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

    private Task<string> CallAsync(string prompt, string operation = "unknown") => CallWithClientAsync(_evalClient, _evalModel, prompt, operation);

    private Task<string> CallLightAsync(string prompt, string operation = "unknown")
    {
        if (_useAnthropicForLight)
            return CallAnthropicAsync(prompt, operation);
        return CallWithClientAsync(_lightClient, _lightModel, prompt, operation);
    }

    private async Task<string> CallAnthropicAsync(string prompt, string operation)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var parameters = new MessageParameters
        {
            Model = _anthropicLightModel!,
            MaxTokens = 4096,
            System = [new SystemMessage("You are a precise JSON-outputting assistant. Always respond with valid JSON only.")],
            Messages = [new Message(RoleType.User, prompt)]
        };

        var response = await _anthropicClient!.Messages.GetClaudeMessageAsync(parameters);
        sw.Stop();

        var inputTokens = response.Usage?.InputTokens ?? 0;
        var outputTokens = response.Usage?.OutputTokens ?? 0;
        var (inputPrice, outputPrice) = ModelPricing.GetValueOrDefault(_anthropicLightModel!, (0.25, 1.25));
        var cost = (inputTokens * inputPrice / 1_000_000) + (outputTokens * outputPrice / 1_000_000);

        var entry = new TokenUsageEntry
        {
            Operation = operation,
            Model = _anthropicLightModel!,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            EstimatedCost = Math.Round(cost, 6),
            DurationMs = sw.Elapsed.TotalMilliseconds,
        };
        lock (_logLock) { _usageLog.Add(entry); }

        var content = response.Content.OfType<TextContent>().FirstOrDefault()?.Text
            ?? throw new InvalidOperationException("No text content in Anthropic response.");

        if (content.StartsWith("```"))
        {
            var firstNewline = content.IndexOf('\n');
            var lastFence = content.LastIndexOf("```");
            if (firstNewline >= 0 && lastFence > firstNewline)
                content = content[(firstNewline + 1)..lastFence].Trim();
        }

        return content;
    }

    private async Task<string> CallWithClientAsync(ChatClient client, string model, string prompt, string operation)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("You are a precise JSON-outputting assistant. Always respond with valid JSON only."),
            new UserChatMessage(prompt)
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
        var completion = await client.CompleteChatAsync(messages, cancellationToken: cts.Token);
        sw.Stop();

        var usage = completion.Value.Usage;
        var inputTokens = usage?.InputTokenCount ?? 0;
        var outputTokens = usage?.OutputTokenCount ?? 0;

        var (inputPrice, outputPrice) = ModelPricing.GetValueOrDefault(model, (2.50, 10.00));
        var cost = (inputTokens * inputPrice / 1_000_000) + (outputTokens * outputPrice / 1_000_000);

        var entry = new TokenUsageEntry
        {
            Operation = operation,
            Model = model,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            EstimatedCost = Math.Round(cost, 6),
            DurationMs = sw.Elapsed.TotalMilliseconds,
        };
        lock (_logLock) { _usageLog.Add(entry); }

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

    public List<TokenUsageEntry> GetTokenUsageLog()
    {
        lock (_logLock) { return [.. _usageLog]; }
    }

    public TokenUsageSummary GetTokenUsageSummary()
    {
        List<TokenUsageEntry> snapshot;
        lock (_logLock) { snapshot = [.. _usageLog]; }

        var byOp = snapshot
            .GroupBy(e => (e.Operation, e.Model))
            .Select(g => new TokenUsageByOperation
            {
                Operation = g.Key.Operation,
                Model = g.Key.Model,
                CallCount = g.Count(),
                TotalInputTokens = g.Sum(e => e.InputTokens),
                TotalOutputTokens = g.Sum(e => e.OutputTokens),
                TotalEstimatedCost = Math.Round(g.Sum(e => e.EstimatedCost), 6),
                AvgDurationMs = Math.Round(g.Average(e => e.DurationMs), 0),
            })
            .OrderByDescending(o => o.TotalEstimatedCost)
            .ToList();

        return new TokenUsageSummary
        {
            TotalCalls = snapshot.Count,
            TotalInputTokens = snapshot.Sum(e => e.InputTokens),
            TotalOutputTokens = snapshot.Sum(e => e.OutputTokens),
            TotalTokens = snapshot.Sum(e => e.TotalTokens),
            TotalEstimatedCost = Math.Round(snapshot.Sum(e => e.EstimatedCost), 6),
            AvgDurationMs = snapshot.Count > 0 ? Math.Round(snapshot.Average(e => e.DurationMs), 0) : 0,
            ByOperation = byOp,
            RecentCalls = snapshot.OrderByDescending(e => e.Timestamp).Take(20).ToList(),
        };
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
