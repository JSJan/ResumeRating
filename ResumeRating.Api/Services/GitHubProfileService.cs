using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace ResumeRating.Api.Services;

public interface IGitHubProfileService
{
    Task<GitHubProfileData> FetchProfileAsync(string username);
    Task<GitHubCodeAnalysis> AnalyzeRepoCodeAsync(string username, List<string> relevantSkills);
}

public class GitHubProfileData
{
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public int PublicRepos { get; set; }
    public int Followers { get; set; }
    public int Following { get; set; }
    public List<GitHubRepoSummary> TopRepos { get; set; } = [];
    public Dictionary<string, int> Languages { get; set; } = new();
    public int TotalStars { get; set; }
    public int TotalForks { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public class GitHubRepoSummary
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int Stars { get; set; }
    public int Forks { get; set; }
    public string UpdatedAt { get; set; } = string.Empty;
    public List<string> Topics { get; set; } = [];
    public string ReadmeExcerpt { get; set; } = string.Empty;
}

public class GitHubCodeAnalysis
{
    public string Username { get; set; } = string.Empty;
    public List<RepoCodeSample> RepoCodeSamples { get; set; } = [];
    public string Summary { get; set; } = string.Empty;
}

public class RepoCodeSample
{
    public string RepoName { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public List<CodeFile> CodeFiles { get; set; } = [];
}

public class CodeFile
{
    public string Path { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int SizeBytes { get; set; }
}

public class GitHubProfileService : IGitHubProfileService
{
    private readonly HttpClient _httpClient;
    private static readonly ConcurrentDictionary<string, (GitHubProfileData Data, DateTime FetchedAt)> _profileCache = new();
    private static readonly ConcurrentDictionary<string, (GitHubCodeAnalysis Data, DateTime FetchedAt)> _codeCache = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public GitHubProfileService(IConfiguration configuration)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ResumeRating/1.0");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

        var token = configuration["GitHub:Token"];
        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<GitHubProfileData> FetchProfileAsync(string username)
    {
        var cacheKey = username.ToLowerInvariant();
        if (_profileCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow - cached.FetchedAt < CacheDuration)
            return cached.Data;

        var profile = new GitHubProfileData { Username = username };

        // Fetch user profile
        var userJson = await FetchJsonAsync($"https://api.github.com/users/{Uri.EscapeDataString(username)}");
        if (userJson == null) return profile;

        profile.Name = userJson["name"]?.ToString() ?? username;
        profile.Bio = userJson["bio"]?.ToString() ?? "";
        profile.PublicRepos = userJson["public_repos"]?.ToObject<int>() ?? 0;
        profile.Followers = userJson["followers"]?.ToObject<int>() ?? 0;
        profile.Following = userJson["following"]?.ToObject<int>() ?? 0;

        // Fetch top repos (sorted by stars, max 10)
        var reposArray = await FetchJsonArrayAsync(
            $"https://api.github.com/users/{Uri.EscapeDataString(username)}/repos?sort=stars&direction=desc&per_page=10&type=owner");

        foreach (var repo in reposArray)
        {
            var repoSummary = new GitHubRepoSummary
            {
                Name = repo["name"]?.ToString() ?? "",
                Description = repo["description"]?.ToString() ?? "",
                Language = repo["language"]?.ToString() ?? "",
                Stars = repo["stargazers_count"]?.ToObject<int>() ?? 0,
                Forks = repo["forks_count"]?.ToObject<int>() ?? 0,
                UpdatedAt = repo["updated_at"]?.ToString() ?? "",
            };

            if (repo["topics"] is Newtonsoft.Json.Linq.JArray topics)
                repoSummary.Topics = topics.Select(t => t.ToString()).ToList();

            // Fetch README excerpt for top 3 repos
            if (profile.TopRepos.Count < 3)
            {
                repoSummary.ReadmeExcerpt = await FetchReadmeExcerptAsync(username, repoSummary.Name);
            }

            profile.TopRepos.Add(repoSummary);

            if (!string.IsNullOrWhiteSpace(repoSummary.Language))
            {
                profile.Languages.TryGetValue(repoSummary.Language, out var count);
                profile.Languages[repoSummary.Language] = count + 1;
            }

            profile.TotalStars += repoSummary.Stars;
            profile.TotalForks += repoSummary.Forks;
        }

        // Build summary
        var langList = string.Join(", ", profile.Languages.OrderByDescending(l => l.Value).Take(5).Select(l => $"{l.Key} ({l.Value} repos)"));
        var topRepoList = string.Join(", ", profile.TopRepos.Take(5).Select(r => $"{r.Name} ({r.Stars}★)"));

        profile.Summary = $"""
            GitHub: {profile.Username} | {profile.Name}
            Bio: {profile.Bio}
            Public Repos: {profile.PublicRepos} | Followers: {profile.Followers} | Total Stars: {profile.TotalStars}
            Top Languages: {langList}
            Top Repos: {topRepoList}
            """;

        // Add repo details with README excerpts
        foreach (var repo in profile.TopRepos.Take(3))
        {
            profile.Summary += $"\n\n--- Repo: {repo.Name} ---\n";
            profile.Summary += $"Description: {repo.Description}\n";
            profile.Summary += $"Language: {repo.Language} | Stars: {repo.Stars} | Forks: {repo.Forks}\n";
            if (repo.Topics.Count > 0)
                profile.Summary += $"Topics: {string.Join(", ", repo.Topics)}\n";
            if (!string.IsNullOrWhiteSpace(repo.ReadmeExcerpt))
                profile.Summary += $"README excerpt:\n{repo.ReadmeExcerpt}\n";
        }

        _profileCache[cacheKey] = (profile, DateTime.UtcNow);
        return profile;
    }

    private async Task<string> FetchReadmeExcerptAsync(string username, string repoName)
    {
        try
        {
            var url = $"https://api.github.com/repos/{Uri.EscapeDataString(username)}/{Uri.EscapeDataString(repoName)}/readme";
            var json = await FetchJsonAsync(url);
            if (json == null) return "";

            var content = json["content"]?.ToString() ?? "";
            var encoding = json["encoding"]?.ToString() ?? "";

            if (encoding == "base64" && !string.IsNullOrWhiteSpace(content))
            {
                var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(content));
                // Return first 500 chars as excerpt
                return decoded.Length > 500 ? decoded[..500] + "..." : decoded;
            }
        }
        catch { /* README not available */ }
        return "";
    }

    private async Task<Newtonsoft.Json.Linq.JObject?> FetchJsonAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;
            var content = await response.Content.ReadAsStringAsync();
            return Newtonsoft.Json.Linq.JObject.Parse(content);
        }
        catch { return null; }
    }

    private async Task<List<Newtonsoft.Json.Linq.JObject>> FetchJsonArrayAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return [];
            var content = await response.Content.ReadAsStringAsync();
            var array = Newtonsoft.Json.Linq.JArray.Parse(content);
            return array.OfType<Newtonsoft.Json.Linq.JObject>().ToList();
        }
        catch { return []; }
    }

    public async Task<GitHubCodeAnalysis> AnalyzeRepoCodeAsync(string username, List<string> relevantSkills)
    {
        // Cache key includes username + sorted skills for consistency
        var skillsKey = string.Join(",", relevantSkills.OrderBy(s => s, StringComparer.OrdinalIgnoreCase));
        var cacheKey = $"{username.ToLowerInvariant()}:{skillsKey}";
        if (_codeCache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow - cached.FetchedAt < CacheDuration)
            return cached.Data;

        var analysis = new GitHubCodeAnalysis { Username = username };

        // Map skills to file extensions that matter
        var extensionMap = BuildExtensionMap(relevantSkills);

        // Fetch repos sorted by recently updated (active repos are more relevant)
        var repos = await FetchJsonArrayAsync(
            $"https://api.github.com/users/{Uri.EscapeDataString(username)}/repos?sort=updated&direction=desc&per_page=10&type=owner");

        // Pick up to 5 repos that have languages matching the JD skills
        var relevantRepos = repos
            .Where(r =>
            {
                var lang = r["language"]?.ToString() ?? "";
                var topics = r["topics"] is Newtonsoft.Json.Linq.JArray t ? t.Select(x => x.ToString().ToLowerInvariant()).ToList() : new List<string>();
                var desc = (r["description"]?.ToString() ?? "").ToLowerInvariant();
                var name = (r["name"]?.ToString() ?? "").ToLowerInvariant();

                return relevantSkills.Any(skill =>
                    lang.Contains(skill, StringComparison.OrdinalIgnoreCase) ||
                    topics.Any(tp => tp.Contains(skill.ToLowerInvariant())) ||
                    desc.Contains(skill.ToLowerInvariant()) ||
                    name.Contains(skill.ToLowerInvariant()));
            })
            .Take(5)
            .ToList();

        // Fallback: if no skill-matched repos, take top 3 by stars
        if (relevantRepos.Count == 0)
            relevantRepos = repos.Take(3).ToList();

        var summaryBuilder = new System.Text.StringBuilder();
        summaryBuilder.AppendLine($"=== Deep Code Analysis for {username} ===\n");

        foreach (var repo in relevantRepos)
        {
            var repoName = repo["name"]?.ToString() ?? "";
            var repoLang = repo["language"]?.ToString() ?? "";

            var sample = new RepoCodeSample
            {
                RepoName = repoName,
                Language = repoLang
            };

            // Fetch the repo tree (recursive, gets all files)
            var treeUrl = $"https://api.github.com/repos/{Uri.EscapeDataString(username)}/{Uri.EscapeDataString(repoName)}/git/trees/HEAD?recursive=1";
            var treeJson = await FetchJsonAsync(treeUrl);
            if (treeJson == null) continue;

            var tree = treeJson["tree"] as Newtonsoft.Json.Linq.JArray;
            if (tree == null) continue;

            // Filter for source code files that match relevant extensions
            var codeFiles = tree
                .Where(f =>
                {
                    var path = f["path"]?.ToString() ?? "";
                    var type = f["type"]?.ToString() ?? "";
                    var size = f["size"]?.ToObject<int>() ?? 0;

                    if (type != "blob") return false;
                    if (size < 100 || size > 50_000) return false; // skip tiny/huge files
                    if (IsIgnoredPath(path)) return false;

                    var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
                    return extensionMap.Contains(ext) || IsCommonSourceExt(ext);
                })
                .OrderByDescending(f => f["size"]?.ToObject<int>() ?? 0) // prioritize substantial files
                .Take(8) // limit API calls per repo
                .ToList();

            summaryBuilder.AppendLine($"\n--- Repo: {repoName} (Primary Language: {repoLang}) ---");

            var totalCodeFetched = 0;
            foreach (var file in codeFiles)
            {
                if (totalCodeFetched > 15_000) break; // cap total code per repo

                var filePath = file["path"]?.ToString() ?? "";
                var fileSize = file["size"]?.ToObject<int>() ?? 0;

                var fileContent = await FetchFileContentAsync(username, repoName, filePath);
                if (string.IsNullOrWhiteSpace(fileContent)) continue;

                // Truncate very long files to first 2000 chars
                var truncated = fileContent.Length > 2000 ? fileContent[..2000] + "\n... [truncated]" : fileContent;

                sample.CodeFiles.Add(new CodeFile
                {
                    Path = filePath,
                    Content = truncated,
                    SizeBytes = fileSize
                });

                summaryBuilder.AppendLine($"\nFile: {filePath} ({fileSize} bytes)");
                summaryBuilder.AppendLine("```");
                summaryBuilder.AppendLine(truncated);
                summaryBuilder.AppendLine("```");

                totalCodeFetched += truncated.Length;
            }

            analysis.RepoCodeSamples.Add(sample);
        }

        analysis.Summary = summaryBuilder.ToString();
        _codeCache[cacheKey] = (analysis, DateTime.UtcNow);
        return analysis;
    }

    private async Task<string> FetchFileContentAsync(string username, string repoName, string filePath)
    {
        try
        {
            var url = $"https://api.github.com/repos/{Uri.EscapeDataString(username)}/{Uri.EscapeDataString(repoName)}/contents/{filePath}";
            var json = await FetchJsonAsync(url);
            if (json == null) return "";

            var content = json["content"]?.ToString() ?? "";
            var encoding = json["encoding"]?.ToString() ?? "";

            if (encoding == "base64" && !string.IsNullOrWhiteSpace(content))
            {
                return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(content));
            }
        }
        catch { /* file not available */ }
        return "";
    }

    private static HashSet<string> BuildExtensionMap(List<string> skills)
    {
        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var skillToExt = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["c#"] = [".cs"],
            ["csharp"] = [".cs"],
            [".net"] = [".cs", ".csproj"],
            ["dotnet"] = [".cs", ".csproj"],
            ["java"] = [".java"],
            ["python"] = [".py"],
            ["javascript"] = [".js", ".jsx"],
            ["typescript"] = [".ts", ".tsx"],
            ["react"] = [".tsx", ".jsx", ".js"],
            ["angular"] = [".ts", ".html"],
            ["vue"] = [".vue", ".js", ".ts"],
            ["go"] = [".go"],
            ["golang"] = [".go"],
            ["rust"] = [".rs"],
            ["ruby"] = [".rb"],
            ["php"] = [".php"],
            ["swift"] = [".swift"],
            ["kotlin"] = [".kt"],
            ["scala"] = [".scala"],
            ["sql"] = [".sql"],
            ["node"] = [".js", ".ts"],
            ["nodejs"] = [".js", ".ts"],
        };

        foreach (var skill in skills)
        {
            foreach (var kvp in skillToExt)
            {
                if (skill.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var ext in kvp.Value) extensions.Add(ext);
                }
            }
        }

        return extensions;
    }

    private static bool IsCommonSourceExt(string ext)
    {
        return ext is ".cs" or ".java" or ".py" or ".js" or ".ts" or ".tsx" or ".jsx"
            or ".go" or ".rs" or ".rb" or ".php" or ".swift" or ".kt" or ".scala"
            or ".cpp" or ".c" or ".h" or ".vue";
    }

    private static bool IsIgnoredPath(string path)
    {
        var lower = path.ToLowerInvariant();
        return lower.Contains("node_modules/") || lower.Contains("bin/") || lower.Contains("obj/")
            || lower.Contains("dist/") || lower.Contains("build/") || lower.Contains(".min.")
            || lower.Contains("vendor/") || lower.Contains("packages/") || lower.Contains("test/fixtures/")
            || lower.Contains("__pycache__/") || lower.Contains(".lock") || lower.Contains("migrations/");
    }
}
