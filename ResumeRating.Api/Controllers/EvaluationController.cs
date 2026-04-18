using Microsoft.AspNetCore.Mvc;
using ResumeRating.Api.Models;
using ResumeRating.Api.Services;
using System.Text.Json;

namespace ResumeRating.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EvaluationController : ControllerBase
{
    private readonly IEvaluationService _evaluationService;
    private readonly IAiService _aiService;

    public EvaluationController(IEvaluationService evaluationService, IAiService aiService)
    {
        _evaluationService = evaluationService;
        _aiService = aiService;
    }

    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] EvaluateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ResumeId) || string.IsNullOrWhiteSpace(request.JobDescriptionId))
            return BadRequest("ResumeId and JobDescriptionId are required.");

        try
        {
            var evaluation = await _evaluationService.EvaluateAsync(request.ResumeId, request.JobDescriptionId);
            return Ok(evaluation);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [HttpPost("evaluate-stream")]
    public async Task EvaluateWithProgress([FromBody] EvaluateRequest request)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        async Task SendEvent(string step, string message, int progress)
        {
            var data = JsonSerializer.Serialize(new { step, message, progress }, CamelCaseOptions);
            await Response.WriteAsync($"data: {data}\n\n");
            await Response.Body.FlushAsync();
        }

        try
        {
            await SendEvent("start", "Starting evaluation...", 0);

            // Load resume and JD
            await SendEvent("loading", "Loading resume and job description...", 10);
            var resumes = await _evaluationService.GetResumesAsync();
            var resume = resumes.Find(r => r.Id == request.ResumeId);
            if (resume == null) { await SendEvent("error", "Resume not found.", 0); return; }

            var jds = await _evaluationService.GetJobDescriptionsAsync();
            var jd = jds.Find(j => j.Id == request.JobDescriptionId);
            if (jd == null) { await SendEvent("error", "Job description not found.", 0); return; }

            // LinkedIn check
            if (!string.IsNullOrWhiteSpace(resume.LinkedInUrl))
            {
                await SendEvent("linkedin", $"Checking LinkedIn profile: {resume.LinkedInUrl}", 20);
                await Task.Delay(500); // brief pause for UX
            }
            else
            {
                await SendEvent("linkedin", "No LinkedIn profile provided — skipping.", 20);
            }

            // GitHub check
            if (!string.IsNullOrWhiteSpace(resume.GitHubUsername))
            {
                await SendEvent("github_profile", $"Fetching GitHub profile for {resume.GitHubUsername}...", 30);
                await Task.Delay(300);
                await SendEvent("github_code", $"Analyzing source code from {resume.GitHubUsername}'s repositories...", 45);
                await Task.Delay(300);
            }
            else
            {
                await SendEvent("github_profile", "No GitHub username provided — skipping code analysis.", 45);
            }

            // AI Evaluation
            await SendEvent("ai_eval", "Running AI evaluation against job description...", 55);
            await SendEvent("ai_eval", "Checking resume authenticity & buzzword analysis...", 65);
            await SendEvent("ai_eval", "Analyzing code proficiency & generating scores...", 75);

            var evaluation = await _evaluationService.EvaluateAsync(request.ResumeId, request.JobDescriptionId);

            await SendEvent("complete", "Evaluation complete!", 100);

            // Send the final result
            var resultData = JsonSerializer.Serialize(new { step = "result", evaluation, progress = 100 }, CamelCaseOptions);
            await Response.WriteAsync($"data: {resultData}\n\n");
            await Response.Body.FlushAsync();
        }
        catch (Exception ex)
        {
            await SendEvent("error", ex.Message, 0);
        }
    }

    [HttpGet("{jobDescriptionId}")]
    public async Task<IActionResult> GetEvaluations(string jobDescriptionId)
    {
        var evaluations = await _evaluationService.GetEvaluationsAsync(jobDescriptionId);
        return Ok(evaluations);
    }

    [HttpDelete("{evaluationId}")]
    public async Task<IActionResult> DeleteEvaluation(string evaluationId)
    {
        try
        {
            await _evaluationService.DeleteEvaluationAsync(evaluationId);
            return Ok(new { message = "Evaluation deleted." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("questionnaire/{evaluationId}")]
    public async Task<IActionResult> GenerateQuestionnaire(string evaluationId)
    {
        try
        {
            var questionnaire = await _evaluationService.GenerateQuestionnaireAsync(evaluationId);
            return Ok(questionnaire);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("l1-feedback")]
    public async Task<IActionResult> SubmitL1Answers([FromBody] L1AnswersRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EvaluationId) || string.IsNullOrWhiteSpace(request.QuestionnaireId))
            return BadRequest("EvaluationId and QuestionnaireId are required.");

        if (request.Answers == null || request.Answers.Count == 0)
            return BadRequest("At least one answer is required.");

        try
        {
            var feedback = await _evaluationService.SubmitL1AnswersAsync(request);
            return Ok(feedback);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("seed")]
    public async Task<IActionResult> SeedFromAssets()
    {
        try
        {
            var result = await _evaluationService.SeedFromAssetsAsync();
            return Ok(result);
        }
        catch (DirectoryNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("evaluate-all/{jobDescriptionId}")]
    public async Task<IActionResult> EvaluateAll(string jobDescriptionId)
    {
        try
        {
            var evaluations = await _evaluationService.EvaluateAllAsync(jobDescriptionId);
            return Ok(evaluations);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("l2-questionnaire/{evaluationId}")]
    public async Task<IActionResult> GenerateL2Questionnaire(string evaluationId)
    {
        try
        {
            var questionnaire = await _evaluationService.GenerateL2QuestionnaireAsync(evaluationId);
            return Ok(questionnaire);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("l2-feedback")]
    public async Task<IActionResult> SubmitL2Answers([FromBody] L2AnswersRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EvaluationId) || string.IsNullOrWhiteSpace(request.L2QuestionnaireId))
            return BadRequest("EvaluationId and L2QuestionnaireId are required.");

        try
        {
            var assessment = await _evaluationService.SubmitL2AnswersAsync(request);
            return Ok(assessment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("token-usage")]
    public IActionResult GetTokenUsageSummary()
    {
        return Ok(_aiService.GetTokenUsageSummary());
    }

    [HttpGet("token-usage/log")]
    public IActionResult GetTokenUsageLog()
    {
        return Ok(_aiService.GetTokenUsageLog());
    }

    public class EvaluateRequest
    {
        public string ResumeId { get; set; } = string.Empty;
        public string JobDescriptionId { get; set; } = string.Empty;
    }
}
