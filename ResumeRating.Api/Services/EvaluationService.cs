using ResumeRating.Api.Models;

namespace ResumeRating.Api.Services;

public interface IEvaluationService
{
    Task<Resume> UploadResumeAsync(Stream fileStream, string fileName, string? linkedInUrl = null, string? gitHubUsername = null);
    Task<List<Resume>> GetResumesAsync();
    Task<JobDescription> CreateJobDescriptionAsync(JobDescription jd);
    Task<List<JobDescription>> GetJobDescriptionsAsync();
    Task<CandidateEvaluation> EvaluateAsync(string resumeId, string jobDescriptionId);
    Task<List<CandidateEvaluation>> EvaluateAllAsync(string jobDescriptionId);
    Task<List<CandidateEvaluation>> GetEvaluationsAsync(string jobDescriptionId);
    Task<Questionnaire> GenerateQuestionnaireAsync(string evaluationId);
    Task<L1Feedback> SubmitL1AnswersAsync(L1AnswersRequest request);
    Task<L2Questionnaire> GenerateL2QuestionnaireAsync(string evaluationId);
    Task<L2Assessment> SubmitL2AnswersAsync(L2AnswersRequest request);
    Task<SeedResult> SeedFromAssetsAsync();
}

public class SeedResult
{
    public int ResumesLoaded { get; set; }
    public string? JobDescriptionId { get; set; }
    public string? JobDescriptionTitle { get; set; }
    public List<string> Errors { get; set; } = [];
}

public class EvaluationService : IEvaluationService
{
    private readonly IResumeParserService _parser;
    private readonly IAiService _ai;
    private readonly IStorageService _storage;
    private readonly IGitHubProfileService _gitHub;

    private const string ResumesCollection = "resumes";
    private const string JobDescriptionsCollection = "job_descriptions";
    private const string EvaluationsCollection = "evaluations";
    private const string QuestionnairesCollection = "questionnaires";
    private const string L1FeedbackCollection = "l1_feedback";
    private const string L2QuestionnairesCollection = "l2_questionnaires";
    private const string L2AssessmentsCollection = "l2_assessments";

    public EvaluationService(IResumeParserService parser, IAiService ai, IStorageService storage, IGitHubProfileService gitHub)
    {
        _parser = parser;
        _ai = ai;
        _storage = storage;
        _gitHub = gitHub;
    }

    public async Task<Resume> UploadResumeAsync(Stream fileStream, string fileName, string? linkedInUrl = null, string? gitHubUsername = null)
    {
        // Save the file
        await _storage.SaveFileAsync(fileName, fileStream);

        // Re-open saved file to extract text
        var filePath = _storage.GetUploadPath(fileName);
        using var readStream = File.OpenRead(filePath);
        var extractedText = await _parser.ExtractTextAsync(readStream, fileName);

        // Fetch GitHub profile if username provided
        var gitHubSummary = "";
        if (!string.IsNullOrWhiteSpace(gitHubUsername))
        {
            try
            {
                var profile = await _gitHub.FetchProfileAsync(gitHubUsername);
                gitHubSummary = profile.Summary;
            }
            catch { /* GitHub fetch is best-effort */ }
        }

        var resume = new Resume
        {
            FileName = fileName,
            CandidateName = ExtractNameHeuristic(extractedText),
            ExtractedText = extractedText,
            LinkedInUrl = linkedInUrl ?? "",
            GitHubUsername = gitHubUsername ?? "",
            GitHubProfileSummary = gitHubSummary
        };

        await _storage.AppendToListAsync(ResumesCollection, resume);
        return resume;
    }

    public async Task<List<Resume>> GetResumesAsync()
    {
        return await _storage.LoadListAsync<Resume>(ResumesCollection);
    }

    public async Task<JobDescription> CreateJobDescriptionAsync(JobDescription jd)
    {
        jd.Id = Guid.NewGuid().ToString();
        jd.CreatedAt = DateTime.UtcNow;
        await _storage.AppendToListAsync(JobDescriptionsCollection, jd);
        return jd;
    }

    public async Task<List<JobDescription>> GetJobDescriptionsAsync()
    {
        return await _storage.LoadListAsync<JobDescription>(JobDescriptionsCollection);
    }

    public async Task<CandidateEvaluation> EvaluateAsync(string resumeId, string jobDescriptionId)
    {
        var resumes = await _storage.LoadListAsync<Resume>(ResumesCollection);
        var resume = resumes.Find(r => r.Id == resumeId)
            ?? throw new KeyNotFoundException($"Resume '{resumeId}' not found.");

        var jds = await _storage.LoadListAsync<JobDescription>(JobDescriptionsCollection);
        var jd = jds.Find(j => j.Id == jobDescriptionId)
            ?? throw new KeyNotFoundException($"Job description '{jobDescriptionId}' not found.");

        // Refresh GitHub data if username exists but summary is empty
        if (!string.IsNullOrWhiteSpace(resume.GitHubUsername) && string.IsNullOrWhiteSpace(resume.GitHubProfileSummary))
        {
            try
            {
                var profile = await _gitHub.FetchProfileAsync(resume.GitHubUsername);
                resume.GitHubProfileSummary = profile.Summary;
            }
            catch { /* best-effort */ }
        }

        // Deep code analysis: fetch actual source code from GitHub repos relevant to JD
        string? codeAnalysisSummary = null;
        if (!string.IsNullOrWhiteSpace(resume.GitHubUsername))
        {
            try
            {
                var skills = ParseSkillsList(jd.RequiredSkills, jd.PreferredSkills);
                var codeAnalysis = await _gitHub.AnalyzeRepoCodeAsync(resume.GitHubUsername, skills);
                if (codeAnalysis.RepoCodeSamples.Count > 0)
                    codeAnalysisSummary = codeAnalysis.Summary;
            }
            catch { /* code analysis is best-effort */ }
        }

        var evaluation = await _ai.EvaluateResumeAsync(
            resume.ExtractedText,
            jd,
            string.IsNullOrWhiteSpace(resume.LinkedInUrl) ? null : resume.LinkedInUrl,
            string.IsNullOrWhiteSpace(resume.GitHubProfileSummary) ? null : resume.GitHubProfileSummary,
            codeAnalysisSummary);
        evaluation.ResumeId = resumeId;
        evaluation.JobDescriptionId = jobDescriptionId;

        await _storage.AppendToListAsync(EvaluationsCollection, evaluation);
        return evaluation;
    }

    public async Task<List<CandidateEvaluation>> GetEvaluationsAsync(string jobDescriptionId)
    {
        var evaluations = await _storage.LoadListAsync<CandidateEvaluation>(EvaluationsCollection);
        return evaluations.Where(e => e.JobDescriptionId == jobDescriptionId).ToList();
    }

    public async Task<Questionnaire> GenerateQuestionnaireAsync(string evaluationId)
    {
        var evaluations = await _storage.LoadListAsync<CandidateEvaluation>(EvaluationsCollection);
        var evaluation = evaluations.Find(e => e.Id == evaluationId)
            ?? throw new KeyNotFoundException($"Evaluation '{evaluationId}' not found.");

        if (!evaluation.RecommendedForL1)
            throw new InvalidOperationException("Candidate was not recommended for L1 round.");

        var resumes = await _storage.LoadListAsync<Resume>(ResumesCollection);
        var resume = resumes.Find(r => r.Id == evaluation.ResumeId)
            ?? throw new KeyNotFoundException("Associated resume not found.");

        var jds = await _storage.LoadListAsync<JobDescription>(JobDescriptionsCollection);
        var jd = jds.Find(j => j.Id == evaluation.JobDescriptionId)
            ?? throw new KeyNotFoundException("Associated job description not found.");

        var questionnaire = await _ai.GenerateQuestionnaireAsync(evaluation, resume.ExtractedText, jd);
        await _storage.AppendToListAsync(QuestionnairesCollection, questionnaire);
        return questionnaire;
    }

    public async Task<L1Feedback> SubmitL1AnswersAsync(L1AnswersRequest request)
    {
        var evaluations = await _storage.LoadListAsync<CandidateEvaluation>(EvaluationsCollection);
        var evaluation = evaluations.Find(e => e.Id == request.EvaluationId)
            ?? throw new KeyNotFoundException($"Evaluation '{request.EvaluationId}' not found.");

        var questionnaires = await _storage.LoadListAsync<Questionnaire>(QuestionnairesCollection);
        var questionnaire = questionnaires.Find(q => q.Id == request.QuestionnaireId)
            ?? throw new KeyNotFoundException($"Questionnaire '{request.QuestionnaireId}' not found.");

        var resumes = await _storage.LoadListAsync<Resume>(ResumesCollection);
        var resume = resumes.Find(r => r.Id == evaluation.ResumeId)
            ?? throw new KeyNotFoundException("Associated resume not found.");

        var jds = await _storage.LoadListAsync<JobDescription>(JobDescriptionsCollection);
        var jd = jds.Find(j => j.Id == evaluation.JobDescriptionId)
            ?? throw new KeyNotFoundException("Associated job description not found.");

        var feedback = await _ai.EvaluateL1AnswersAsync(evaluation, questionnaire, request.Answers, resume.ExtractedText, jd);
        await _storage.AppendToListAsync(L1FeedbackCollection, feedback);
        return feedback;
    }

    public async Task<List<CandidateEvaluation>> EvaluateAllAsync(string jobDescriptionId)
    {
        var jds = await _storage.LoadListAsync<JobDescription>(JobDescriptionsCollection);
        var jd = jds.Find(j => j.Id == jobDescriptionId)
            ?? throw new KeyNotFoundException($"Job description '{jobDescriptionId}' not found.");

        var resumes = await _storage.LoadListAsync<Resume>(ResumesCollection);
        var existingEvals = await _storage.LoadListAsync<CandidateEvaluation>(EvaluationsCollection);
        var alreadyEvaluated = existingEvals
            .Where(e => e.JobDescriptionId == jobDescriptionId)
            .Select(e => e.ResumeId)
            .ToHashSet();

        var results = new List<CandidateEvaluation>();

        // Only evaluate resumes that haven't been evaluated for this JD yet
        foreach (var resume in resumes.Where(r => !alreadyEvaluated.Contains(r.Id)))
        {
            // Fetch GitHub profile and code analysis if available
            string? gitHubSummary = resume.GitHubProfileSummary;
            string? codeAnalysisSummary = null;

            if (!string.IsNullOrWhiteSpace(resume.GitHubUsername))
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(gitHubSummary))
                    {
                        var profile = await _gitHub.FetchProfileAsync(resume.GitHubUsername);
                        gitHubSummary = profile.Summary;
                    }

                    var skills = ParseSkillsList(jd.RequiredSkills, jd.PreferredSkills);
                    var codeAnalysis = await _gitHub.AnalyzeRepoCodeAsync(resume.GitHubUsername, skills);
                    if (codeAnalysis.RepoCodeSamples.Count > 0)
                        codeAnalysisSummary = codeAnalysis.Summary;
                }
                catch { /* best-effort */ }
            }

            var evaluation = await _ai.EvaluateResumeAsync(
                resume.ExtractedText,
                jd,
                string.IsNullOrWhiteSpace(resume.LinkedInUrl) ? null : resume.LinkedInUrl,
                gitHubSummary,
                codeAnalysisSummary);
            evaluation.ResumeId = resume.Id;
            evaluation.JobDescriptionId = jobDescriptionId;
            await _storage.AppendToListAsync(EvaluationsCollection, evaluation);
            results.Add(evaluation);
        }

        // Return all evaluations for this JD (existing + new)
        var allEvals = await _storage.LoadListAsync<CandidateEvaluation>(EvaluationsCollection);
        return allEvals.Where(e => e.JobDescriptionId == jobDescriptionId).ToList();
    }

    public async Task<L2Questionnaire> GenerateL2QuestionnaireAsync(string evaluationId)
    {
        var evaluations = await _storage.LoadListAsync<CandidateEvaluation>(EvaluationsCollection);
        var evaluation = evaluations.Find(e => e.Id == evaluationId)
            ?? throw new KeyNotFoundException($"Evaluation '{evaluationId}' not found.");

        var l1Feedbacks = await _storage.LoadListAsync<L1Feedback>(L1FeedbackCollection);
        var l1Feedback = l1Feedbacks.Find(f => f.EvaluationId == evaluationId)
            ?? throw new KeyNotFoundException("No L1 feedback found for this evaluation. Complete L1 round first.");

        if (!l1Feedback.RecommendedForTechRound)
            throw new InvalidOperationException("Candidate was not recommended for tech hands-on round.");

        var resumes = await _storage.LoadListAsync<Resume>(ResumesCollection);
        var resume = resumes.Find(r => r.Id == evaluation.ResumeId)
            ?? throw new KeyNotFoundException("Associated resume not found.");

        var jds = await _storage.LoadListAsync<JobDescription>(JobDescriptionsCollection);
        var jd = jds.Find(j => j.Id == evaluation.JobDescriptionId)
            ?? throw new KeyNotFoundException("Associated job description not found.");

        var questionnaire = await _ai.GenerateL2QuestionnaireAsync(evaluation, l1Feedback, resume.ExtractedText, jd);
        await _storage.AppendToListAsync(L2QuestionnairesCollection, questionnaire);
        return questionnaire;
    }

    public async Task<L2Assessment> SubmitL2AnswersAsync(L2AnswersRequest request)
    {
        var evaluations = await _storage.LoadListAsync<CandidateEvaluation>(EvaluationsCollection);
        var evaluation = evaluations.Find(e => e.Id == request.EvaluationId)
            ?? throw new KeyNotFoundException($"Evaluation '{request.EvaluationId}' not found.");

        var l2Questionnaires = await _storage.LoadListAsync<L2Questionnaire>(L2QuestionnairesCollection);
        var questionnaire = l2Questionnaires.Find(q => q.Id == request.L2QuestionnaireId)
            ?? throw new KeyNotFoundException($"L2 Questionnaire '{request.L2QuestionnaireId}' not found.");

        var resumes = await _storage.LoadListAsync<Resume>(ResumesCollection);
        var resume = resumes.Find(r => r.Id == evaluation.ResumeId)
            ?? throw new KeyNotFoundException("Associated resume not found.");

        var jds = await _storage.LoadListAsync<JobDescription>(JobDescriptionsCollection);
        var jd = jds.Find(j => j.Id == evaluation.JobDescriptionId)
            ?? throw new KeyNotFoundException("Associated job description not found.");

        var assessment = await _ai.EvaluateL2AnswersAsync(evaluation, questionnaire, request, resume.ExtractedText, jd);
        await _storage.AppendToListAsync(L2AssessmentsCollection, assessment);
        return assessment;
    }

    private static string ExtractNameHeuristic(string text)
    {
        // Simple heuristic: first non-empty line is likely the name
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return lines.Length > 0 ? lines[0].Trim() : "Unknown";
    }

    private static List<string> ParseSkillsList(string requiredSkills, string preferredSkills)
    {
        var combined = $"{requiredSkills}, {preferredSkills}";
        return combined
            .Split(new[] { ',', ';', '|', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<SeedResult> SeedFromAssetsAsync()
    {
        var result = new SeedResult();
        var assetsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "assets");

        if (!Directory.Exists(assetsPath))
            throw new DirectoryNotFoundException($"Assets folder not found at: {Path.GetFullPath(assetsPath)}");

        // Load job description from markdown (skip if same title already exists)
        var jdPath = Path.Combine(assetsPath, "job-description.md");
        if (File.Exists(jdPath))
        {
            var mdContent = await File.ReadAllTextAsync(jdPath);
            var jd = ParseJobDescriptionMarkdown(mdContent);

            var existingJds = await _storage.LoadListAsync<JobDescription>(JobDescriptionsCollection);
            var duplicate = existingJds.Find(j => j.Title.Equals(jd.Title, StringComparison.OrdinalIgnoreCase));
            if (duplicate != null)
            {
                result.JobDescriptionId = duplicate.Id;
                result.JobDescriptionTitle = duplicate.Title;
            }
            else
            {
                jd = await CreateJobDescriptionAsync(jd);
                result.JobDescriptionId = jd.Id;
                result.JobDescriptionTitle = jd.Title;
            }
        }
        else
        {
            result.Errors.Add("job-description.md not found in assets folder.");
        }

        // Load all resumes (PDF, DOCX, TXT) from assets/resumes subfolder (fallback to assets root)
        var resumesPath = Path.Combine(assetsPath, "resumes");
        var resumeSearchPath = Directory.Exists(resumesPath) ? resumesPath : assetsPath;
        var supportedExtensions = new[] { ".pdf", ".docx", ".txt" };
        var resumeFiles = Directory.GetFiles(resumeSearchPath)
            .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToArray();

        // Skip resumes already loaded (by filename) to avoid duplicates
        var existingResumes = await _storage.LoadListAsync<Resume>(ResumesCollection);
        var existingFileNames = existingResumes.Select(r => r.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var pdfFile in resumeFiles)
        {
            try
            {
                var fileName = Path.GetFileName(pdfFile);
                if (existingFileNames.Contains(fileName))
                    continue;

                using var stream = File.OpenRead(pdfFile);
                var extractedText = await _parser.ExtractTextAsync(stream, fileName);

                var resume = new Resume
                {
                    FileName = fileName,
                    CandidateName = ExtractNameHeuristic(extractedText),
                    ExtractedText = extractedText
                };

                await _storage.AppendToListAsync(ResumesCollection, resume);
                result.ResumesLoaded++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to load {Path.GetFileName(pdfFile)}: {ex.Message}");
            }
        }

        return result;
    }

    private static JobDescription ParseJobDescriptionMarkdown(string markdown)
    {
        var jd = new JobDescription();
        var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? currentSection = null;
        var sectionContent = new System.Text.StringBuilder();

        foreach (var line in markdown.Split('\n'))
        {
            if (line.StartsWith("## "))
            {
                if (currentSection != null)
                    sections[currentSection] = sectionContent.ToString().Trim();
                currentSection = line[3..].Trim();
                sectionContent.Clear();
            }
            else if (line.StartsWith("# "))
            {
                jd.Title = line[2..].Trim();
            }
            else if (currentSection != null)
            {
                sectionContent.AppendLine(line);
            }
        }
        if (currentSection != null)
            sections[currentSection] = sectionContent.ToString().Trim();

        if (sections.TryGetValue("About the Role", out var about))
            jd.Description = about;
        else if (sections.TryGetValue("Responsibilities", out var resp))
            jd.Description = resp;

        if (sections.TryGetValue("Required Skills", out var req))
            jd.RequiredSkills = req;

        if (sections.TryGetValue("Preferred Skills", out var pref))
            jd.PreferredSkills = pref;

        if (sections.TryGetValue("Experience Level", out var exp))
            jd.ExperienceLevel = exp;

        return jd;
    }
}
