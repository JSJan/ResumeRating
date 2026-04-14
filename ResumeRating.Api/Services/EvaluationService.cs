using ResumeRating.Api.Models;

namespace ResumeRating.Api.Services;

public interface IEvaluationService
{
    Task<Resume> UploadResumeAsync(Stream fileStream, string fileName);
    Task<List<Resume>> GetResumesAsync();
    Task<JobDescription> CreateJobDescriptionAsync(JobDescription jd);
    Task<List<JobDescription>> GetJobDescriptionsAsync();
    Task<CandidateEvaluation> EvaluateAsync(string resumeId, string jobDescriptionId);
    Task<List<CandidateEvaluation>> GetEvaluationsAsync(string jobDescriptionId);
    Task<Questionnaire> GenerateQuestionnaireAsync(string evaluationId);
    Task<L1Feedback> SubmitL1AnswersAsync(L1AnswersRequest request);
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

    private const string ResumesCollection = "resumes";
    private const string JobDescriptionsCollection = "job_descriptions";
    private const string EvaluationsCollection = "evaluations";
    private const string QuestionnairesCollection = "questionnaires";
    private const string L1FeedbackCollection = "l1_feedback";

    public EvaluationService(IResumeParserService parser, IAiService ai, IStorageService storage)
    {
        _parser = parser;
        _ai = ai;
        _storage = storage;
    }

    public async Task<Resume> UploadResumeAsync(Stream fileStream, string fileName)
    {
        // Save the file
        await _storage.SaveFileAsync(fileName, fileStream);

        // Re-open saved file to extract text
        var filePath = _storage.GetUploadPath(fileName);
        using var readStream = File.OpenRead(filePath);
        var extractedText = await _parser.ExtractTextAsync(readStream, fileName);

        var resume = new Resume
        {
            FileName = fileName,
            CandidateName = ExtractNameHeuristic(extractedText),
            ExtractedText = extractedText
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

        var evaluation = await _ai.EvaluateResumeAsync(resume.ExtractedText, jd);
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

    private static string ExtractNameHeuristic(string text)
    {
        // Simple heuristic: first non-empty line is likely the name
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return lines.Length > 0 ? lines[0].Trim() : "Unknown";
    }

    public async Task<SeedResult> SeedFromAssetsAsync()
    {
        var result = new SeedResult();
        var assetsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "assets");

        if (!Directory.Exists(assetsPath))
            throw new DirectoryNotFoundException($"Assets folder not found at: {Path.GetFullPath(assetsPath)}");

        // Load job description from markdown
        var jdPath = Path.Combine(assetsPath, "job-description.md");
        if (File.Exists(jdPath))
        {
            var mdContent = await File.ReadAllTextAsync(jdPath);
            var jd = ParseJobDescriptionMarkdown(mdContent);
            jd = await CreateJobDescriptionAsync(jd);
            result.JobDescriptionId = jd.Id;
            result.JobDescriptionTitle = jd.Title;
        }
        else
        {
            result.Errors.Add("job-description.md not found in assets folder.");
        }

        // Load all PDFs from assets
        var pdfFiles = Directory.GetFiles(assetsPath, "*.pdf");
        foreach (var pdfFile in pdfFiles)
        {
            try
            {
                using var stream = File.OpenRead(pdfFile);
                var fileName = Path.GetFileName(pdfFile);
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
