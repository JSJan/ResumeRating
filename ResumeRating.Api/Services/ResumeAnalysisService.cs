using System.Text.RegularExpressions;

namespace ResumeRating.Api.Services;

/// <summary>
/// Local pre-processing pipeline that computes resume analysis metrics
/// without using AI, reducing token usage and cost.
/// </summary>
public interface IResumeAnalysisService
{
    ResumeAnalysis Analyze(string resumeText, string jobDescriptionText, string requiredSkills, string preferredSkills);
    GitHubSkillMatch ComputeGitHubSkillMatch(GitHubProfileData profile, string requiredSkills, string preferredSkills);
}

public class ResumeAnalysis
{
    public double KeywordMatchPercent { get; set; }
    public Dictionary<string, int> BuzzwordCounts { get; set; } = new();
    public List<string> MatchedSkills { get; set; } = [];
    public List<string> MissingSkills { get; set; } = [];
    public int EstimatedYearsOfExperience { get; set; }
    public string EducationLevel { get; set; } = "Unknown";
    public double ReadabilityScore { get; set; }
    public double SentenceLengthVariance { get; set; }
    public int PowerVerbCount { get; set; }
    public List<string> PowerVerbsFound { get; set; } = [];
    public double AiGeneratedProbability { get; set; }
    public int TotalWordCount { get; set; }
    public int UniqueWordCount { get; set; }
}

public class GitHubSkillMatch
{
    public double OverlapPercent { get; set; }
    public List<string> MatchedLanguages { get; set; } = [];
    public List<string> MatchedTopics { get; set; } = [];
    public int TotalRepos { get; set; }
    public int TotalStars { get; set; }
}

public partial class ResumeAnalysisService : IResumeAnalysisService
{
    private static readonly HashSet<string> PowerVerbs = new(StringComparer.OrdinalIgnoreCase)
    {
        "spearheaded", "orchestrated", "championed", "pioneered", "leveraged",
        "revolutionized", "synergized", "streamlined", "architected", "transformed",
        "drove", "accelerated", "empowered", "optimized", "implemented",
        "overhauled", "elevated", "conceptualized", "galvanized", "operationalized"
    };

    private static readonly HashSet<string> GenericPhrases = new(StringComparer.OrdinalIgnoreCase)
    {
        "results-driven", "passionate about technology", "team player",
        "self-starter", "detail-oriented", "fast learner", "highly motivated",
        "proven track record", "strong communicator", "dynamic professional"
    };

    public ResumeAnalysis Analyze(string resumeText, string jobDescriptionText, string requiredSkills, string preferredSkills)
    {
        var analysis = new ResumeAnalysis();

        var resumeWords = TokenizeWords(resumeText);
        analysis.TotalWordCount = resumeWords.Length;
        analysis.UniqueWordCount = resumeWords.Distinct(StringComparer.OrdinalIgnoreCase).Count();

        // 1. Keyword match: extract JD skills, count occurrences in resume
        var skills = ParseSkills(requiredSkills, preferredSkills);
        foreach (var skill in skills)
        {
            var count = CountOccurrences(resumeText, skill);
            if (count > 0)
            {
                analysis.MatchedSkills.Add(skill);
                if (count >= 3)
                    analysis.BuzzwordCounts[skill] = count;
            }
            else
            {
                analysis.MissingSkills.Add(skill);
            }
        }
        analysis.KeywordMatchPercent = skills.Count > 0
            ? (double)analysis.MatchedSkills.Count / skills.Count * 100
            : 0;

        // 2. Years of experience extraction
        analysis.EstimatedYearsOfExperience = ExtractYearsOfExperience(resumeText);

        // 3. Education level detection
        analysis.EducationLevel = DetectEducationLevel(resumeText);

        // 4. Readability score (Flesch-Kincaid approximation)
        var sentences = SplitSentences(resumeText);
        var syllables = resumeWords.Sum(w => CountSyllables(w));
        if (sentences.Length > 0 && resumeWords.Length > 0)
        {
            analysis.ReadabilityScore = 206.835
                - 1.015 * ((double)resumeWords.Length / sentences.Length)
                - 84.6 * ((double)syllables / resumeWords.Length);
        }

        // 5. Sentence length variance (low variance = AI-generated signal)
        if (sentences.Length > 2)
        {
            var sentenceLengths = sentences.Select(s => TokenizeWords(s).Length).ToArray();
            var mean = sentenceLengths.Average();
            analysis.SentenceLengthVariance = sentenceLengths.Sum(l => Math.Pow(l - mean, 2)) / sentenceLengths.Length;
        }

        // 6. Power verb detection
        foreach (var verb in PowerVerbs)
        {
            var count = CountOccurrences(resumeText, verb);
            if (count > 0)
            {
                analysis.PowerVerbCount += count;
                analysis.PowerVerbsFound.Add($"{verb} ({count})");
            }
        }

        // 7. AI-generated probability (heuristic composite score 0-1)
        analysis.AiGeneratedProbability = ComputeAiProbability(analysis, resumeText);

        return analysis;
    }

    public GitHubSkillMatch ComputeGitHubSkillMatch(GitHubProfileData profile, string requiredSkills, string preferredSkills)
    {
        var skills = ParseSkills(requiredSkills, preferredSkills);
        var match = new GitHubSkillMatch
        {
            TotalRepos = profile.PublicRepos,
            TotalStars = profile.TotalStars
        };

        foreach (var lang in profile.Languages.Keys)
        {
            if (skills.Any(s => lang.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                                s.Contains(lang, StringComparison.OrdinalIgnoreCase)))
            {
                match.MatchedLanguages.Add(lang);
            }
        }

        foreach (var repo in profile.TopRepos)
        {
            foreach (var topic in repo.Topics)
            {
                if (skills.Any(s => topic.Contains(s, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!match.MatchedTopics.Contains(topic, StringComparer.OrdinalIgnoreCase))
                        match.MatchedTopics.Add(topic);
                }
            }
        }

        var totalMatches = match.MatchedLanguages.Count + match.MatchedTopics.Count;
        match.OverlapPercent = skills.Count > 0 ? Math.Min(100, (double)totalMatches / skills.Count * 100) : 0;
        return match;
    }

    private static double ComputeAiProbability(ResumeAnalysis analysis, string text)
    {
        double score = 0;
        int factors = 0;

        // Low sentence variance suggests AI
        if (analysis.SentenceLengthVariance < 15 && analysis.TotalWordCount > 100)
        {
            score += 0.3;
            factors++;
        }

        // High power verb density
        if (analysis.TotalWordCount > 0 && (double)analysis.PowerVerbCount / analysis.TotalWordCount > 0.01)
        {
            score += 0.25;
            factors++;
        }

        // Generic phrases
        var genericCount = GenericPhrases.Count(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));
        if (genericCount >= 3)
        {
            score += 0.25;
            factors++;
        }

        // Very high unique word ratio suggests templated/polished text
        if (analysis.TotalWordCount > 50)
        {
            var ratio = (double)analysis.UniqueWordCount / analysis.TotalWordCount;
            if (ratio > 0.75)
            {
                score += 0.2;
                factors++;
            }
        }

        return factors > 0 ? Math.Min(1.0, score) : 0;
    }

    private static List<string> ParseSkills(string required, string preferred)
    {
        var combined = $"{required}, {preferred}";
        return combined
            .Split([',', ';', '|', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int CountOccurrences(string text, string word)
    {
        var pattern = $@"\b{Regex.Escape(word)}\b";
        return Regex.Matches(text, pattern, RegexOptions.IgnoreCase).Count;
    }

    private static string[] TokenizeWords(string text)
    {
        return WordTokenizer().Matches(text).Select(m => m.Value).ToArray();
    }

    private static string[] SplitSentences(string text)
    {
        return SentenceSplitter().Split(text)
            .Where(s => s.Trim().Length > 5)
            .ToArray();
    }

    private static int ExtractYearsOfExperience(string text)
    {
        // Match patterns like "10+ years", "10 years of experience", "over 8 years"
        var match = YearsOfExperiencePattern().Match(text);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var years))
            return years;

        // Fallback: count date ranges (2015-2020 etc.)
        var dateRanges = DateRangePattern().Matches(text);
        if (dateRanges.Count > 0)
        {
            var maxSpan = 0;
            foreach (Match dr in dateRanges)
            {
                if (int.TryParse(dr.Groups[1].Value, out var start) && int.TryParse(dr.Groups[2].Value, out var end))
                    maxSpan = Math.Max(maxSpan, end - start);
            }
            return maxSpan;
        }

        return 0;
    }

    private static string DetectEducationLevel(string text)
    {
        var lower = text.ToLowerInvariant();
        if (lower.Contains("ph.d") || lower.Contains("phd") || lower.Contains("doctorate"))
            return "PhD";
        if (lower.Contains("master") || lower.Contains("m.s.") || lower.Contains("m.sc") || lower.Contains("mba") || lower.Contains("m.tech"))
            return "Masters";
        if (lower.Contains("bachelor") || lower.Contains("b.s.") || lower.Contains("b.sc") || lower.Contains("b.tech") || lower.Contains("b.e."))
            return "Bachelors";
        if (lower.Contains("associate") || lower.Contains("diploma"))
            return "Associate/Diploma";
        return "Unknown";
    }

    private static int CountSyllables(string word)
    {
        if (word.Length <= 3) return 1;
        word = word.ToLowerInvariant();
        var count = VowelPattern().Matches(word).Count;
        if (word.EndsWith('e')) count--;
        return Math.Max(1, count);
    }

    [GeneratedRegex(@"\w+")]
    private static partial Regex WordTokenizer();

    [GeneratedRegex(@"[.!?]+\s+")]
    private static partial Regex SentenceSplitter();

    [GeneratedRegex(@"(\d{1,2})\+?\s*(?:years|yrs)", RegexOptions.IgnoreCase)]
    private static partial Regex YearsOfExperiencePattern();

    [GeneratedRegex(@"(20\d{2})\s*[-–]\s*(20\d{2})")]
    private static partial Regex DateRangePattern();

    [GeneratedRegex(@"[aeiouy]+")]
    private static partial Regex VowelPattern();
}
