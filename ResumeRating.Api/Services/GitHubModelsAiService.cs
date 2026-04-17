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

    public async Task<CandidateEvaluation> EvaluateResumeAsync(string resumeText, JobDescription jobDescription, string? linkedInUrl = null, string? gitHubSummary = null, string? codeAnalysis = null)
    {
        var linkedInSection = !string.IsNullOrWhiteSpace(linkedInUrl)
            ? $"\n## LinkedIn Profile (for authenticity cross-reference only — do NOT heavily weight this)\n**URL:** {linkedInUrl}\n(Use ONLY to verify resume authenticity: does the career timeline match? Are claimed roles consistent? Do NOT score LinkedIn profile quality, completeness, or endorsements as a major factor.)\n"
            : "\n## LinkedIn Profile\nNot provided — this should NOT negatively impact scoring.\n";

        var gitHubSection = !string.IsNullOrWhiteSpace(gitHubSummary)
            ? $"\n## GitHub Profile & Contributions (BONUS — use to increase rating if impressive)\n{gitHubSummary}\n"
            : "\n## GitHub Profile\nNot provided — this should NOT negatively impact scoring.\n";

        var codeAnalysisSection = !string.IsNullOrWhiteSpace(codeAnalysis)
            ? $"\n## Actual Source Code from Candidate's GitHub Repos\nBelow is real source code fetched from the candidate's public repositories. Analyze this to assess code quality, design patterns, naming conventions, architecture decisions, error handling, innovation, and actual proficiency level.\n\n{codeAnalysis}\n"
            : "\n## Source Code Analysis\nNo GitHub code available for analysis.\n";

        var prompt = $$"""
            You are an expert technical recruiter, hiring manager, and senior code reviewer. Evaluate the following resume against the provided job description.
            Also consider the candidate's online presence (LinkedIn profile and GitHub contributions) as part of the evaluation.

            ## Job Description
            **Title:** {{jobDescription.Title}}
            **Description:** {{jobDescription.Description}}
            **Required Skills:** {{jobDescription.RequiredSkills}}
            **Preferred Skills:** {{jobDescription.PreferredSkills}}
            **Experience Level:** {{jobDescription.ExperienceLevel}}

            ## Resume
            {{resumeText}}
            {{linkedInSection}}
            {{gitHubSection}}
            {{codeAnalysisSection}}

            ## CRITICAL: Resume Authenticity & Tailoring Detection
            Resumes are commonly tailored or "gamed" to match job descriptions using AI tools, keyword stuffing, or ATS optimization. Perform a thorough authenticity analysis:

            1. **Keyword density analysis**: Are JD keywords repeated unnaturally? Do buzzwords appear without supporting context or examples?
            2. **Specificity vs. vagueness**: Does the resume contain specific, verifiable achievements (metrics, project names, tech stack details) or generic/vague claims?
            3. **Consistency check**: Do claimed skills match the depth of experience described? Does a "5-year React developer" show commensurate project complexity?
            4. **Cross-reference with GitHub**: If code is available, does the candidate's actual coding proficiency match resume claims? Are claimed technologies actually used in their repos?
            5. **Temporal plausibility**: Are the number of technologies, roles, and achievements plausible for the timeline presented?
            6. **AI-generated resume signals**: Look for overly polished, generic phrasing typical of AI-generated resumes ("leveraged", "spearheaded", "drove", "orchestrated" without specifics).

            ## CRITICAL: Buzzword Stuffing & AI-Overuse Detection
            Many candidates inflate their resumes using buzzwords to game ATS systems or fool recruiters. Detect and penalize:

            1. **Buzzword repetition**: Count how many times JD-matching keywords are repeated. Flag any keyword that appears 3+ times without different context each time.
            2. **Buzzword-to-substance ratio**: If the resume has 20 buzzwords but only 2 concrete examples, that's a red flag. Each claimed skill should have at least one supporting experience.
            3. **Copy-paste from JD**: Are phrases lifted directly from the job description? Compare sentence structure and phrasing patterns.
            4. **AI-generated content detection**: Look for these AI writing telltale signs:
               - Uniform sentence length and structure
               - Excessive use of power verbs without measurable outcomes ("spearheaded", "orchestrated", "championed", "pioneered")
               - Perfect grammar with no personal voice or style
               - Generic descriptions that could apply to any candidate ("results-driven professional", "passionate about technology")
               - Suspiciously comprehensive skill lists that cover every JD requirement perfectly
            5. **Plagiarism signals**: Does the resume read like a job description template? Are role descriptions suspiciously similar to standard LinkedIn job postings or JD templates?
            6. **Authenticity indicators (POSITIVE)**: Specific project names, exact metrics ("reduced latency by 32ms"), named tools/internal systems, unique experiences, personal voice, imperfect but honest phrasing.

            ## CRITICAL: Deep Code Proficiency Assessment
            If source code from GitHub repos is provided, perform a thorough code review:
            1. **Code quality**: Naming conventions, readability, structure, modularity
            2. **Design patterns**: Are established patterns used appropriately?
            3. **Error handling**: Is error handling present and sensible?
            4. **Innovation**: Any creative or novel approaches?
            5. **Relevance**: How relevant is the code to the job description skills?
            6. **Proficiency level**: Based on actual code, what is the true proficiency level (junior/mid/senior/staff)?

            ## SCORING WEIGHTAGE RULES
            - **LinkedIn**: LOW weight. Use ONLY for authenticity cross-referencing (verifying resume claims, career timeline consistency). Do NOT heavily score LinkedIn profile quality, endorsements, or activity. Score onlinePresenceScore as 5/10 baseline.
            - **GitHub (if available)**: BONUS only. Good GitHub repos/code should INCREASE the candidate's scores (especially sideProjects, codeProficiency, and overall). Absence of GitHub should NOT decrease any score — default to neutral (5/10).
            - **Resume + JD fit**: PRIMARY scoring criteria. Focus on experience, work history, education, skills match, and authenticity.

            ## Instructions
            Rate each category from 1-10 and provide detailed feedback.

            Respond in this exact JSON format:
            {
                "candidateName": "<extract from resume>",
                "experienceScore": <1-10>,
                "experienceFeedback": "<detailed feedback>",
                "workHistoryScore": <1-10>,
                "workHistoryFeedback": "<detailed feedback>",
                "educationScore": <1-10>,
                "educationFeedback": "<detailed feedback>",
                "sideProjectsScore": <1-10>,
                "sideProjectsFeedback": "<detailed feedback including GitHub projects analysis>",
                "jobFitScore": <1-10>,
                "jobFitFeedback": "<detailed feedback>",
                "awwFactorScore": <1-10>,
                "awwFactorFeedback": "<what makes this candidate impressive>",
                "uniquenessFactor": <1-10>,
                "uniquenessFeedback": "<what sets them apart from typical candidates>",
                "gitHubScore": <1-10>,
                "gitHubFeedback": "<assessment of GitHub contributions: repo quality, languages, activity, open-source involvement, code samples from READMEs>",
                "onlinePresenceScore": <1-10>,
                "onlinePresenceFeedback": "<assessment of LinkedIn profile, professional branding, consistency between resume and online profiles>",
                "codeProficiencyScore": <1-10>,
                "codeProficiencyFeedback": "<deep analysis of actual source code: quality, patterns, architecture, naming, error handling, innovation, true skill level vs resume claims>",
                "resumeAuthenticityScore": <1-10>,
                "resumeAuthenticityFeedback": "<honest assessment: 10=clearly authentic with verifiable specifics, 1=heavily gamed/keyword-stuffed>",
                "buzzwordScore": <1-10>,
                "buzzwordFeedback": "<10=no buzzword abuse, natural language; 1=heavily stuffed with repeated keywords. List specific buzzwords found with counts>",
                "aiGeneratedScore": <1-10>,
                "aiGeneratedFeedback": "<10=clearly human-written with personal voice; 1=almost certainly AI-generated. Cite specific patterns detected: uniform sentence structure, power verb overuse, generic phrasing, perfect coverage of JD>",
                "tailoringRedFlags": ["<red flag 1>", "<red flag 2>"],
                "buzzwordsDetected": ["<buzzword1 (count)>", "<buzzword2 (count)>"],
                "authenticityAnalysis": "<detailed paragraph: keyword density issues, vague vs specific claims, consistency gaps between resume and code, temporal plausibility concerns, AI-generated content signals, plagiarism indicators>",
                "overallScore": <1-10>,
                "overallFeedback": "<comprehensive summary including authenticity and code proficiency assessment>",
                "standout": "<key differentiators that set this candidate apart>",
                "estimatedCurrentPackage": "<estimate based on experience and skills>",
                "estimatedCurrentRole": "<likely current role/title>",
                "expectedSalaryRange": "<expected salary range for this job>",
                "recommendedForL1": <true/false>
            }

            IMPORTANT:
            - The resumeAuthenticityScore should significantly penalize resumes that appear heavily tailored/gamed. A resume with specific, verifiable achievements and consistent GitHub evidence should score high. A resume full of buzzwords with no substance should score low.
            - The buzzwordScore should penalize keyword stuffing. Count each JD-matching buzzword and flag repeats. A candidate who mentions "microservices" 6 times without 6 different microservices contexts is stuffing.
            - The aiGeneratedScore should detect AI-written resumes. Look for uniform structure, power verbs without metrics, generic phrasing, and suspiciously perfect JD coverage. A human-written resume has personality, imperfections, and specific details.
            - These three scores (authenticity, buzzword, AI) should REDUCE the overallScore if they are low. A beautifully written but fake resume should score LOWER overall than an imperfect but genuine one.
            SCORING DEFAULTS when data is not provided:
            - LinkedIn not provided: onlinePresenceScore = 5/10, feedback: "Not provided — neutral, does not impact scoring."
            - GitHub not provided: gitHubScore = 5/10, codeProficiencyScore = 5/10, feedback: "Not provided — neutral, does not impact scoring."
            - GitHub IS provided with strong code: BOOST gitHubScore, codeProficiencyScore, sideProjectsScore, and overallScore upward. GitHub evidence is a hands-on proof point that should elevate the candidate.
            - LinkedIn IS provided: Use ONLY for authenticity verification. If LinkedIn contradicts resume claims, flag it in resumeAuthenticityFeedback.
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
