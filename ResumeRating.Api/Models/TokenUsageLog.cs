namespace ResumeRating.Api.Models;

public class TokenUsageEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Operation { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens => InputTokens + OutputTokens;
    public double EstimatedCost { get; set; }
    public double DurationMs { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class TokenUsageSummary
{
    public int TotalCalls { get; set; }
    public int TotalInputTokens { get; set; }
    public int TotalOutputTokens { get; set; }
    public int TotalTokens { get; set; }
    public double TotalEstimatedCost { get; set; }
    public double AvgDurationMs { get; set; }
    public List<TokenUsageByOperation> ByOperation { get; set; } = [];
    public List<TokenUsageEntry> RecentCalls { get; set; } = [];
}

public class TokenUsageByOperation
{
    public string Operation { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int CallCount { get; set; }
    public int TotalInputTokens { get; set; }
    public int TotalOutputTokens { get; set; }
    public double TotalEstimatedCost { get; set; }
    public double AvgDurationMs { get; set; }
}
