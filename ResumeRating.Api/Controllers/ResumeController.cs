using Microsoft.AspNetCore.Mvc;
using ResumeRating.Api.Services;

namespace ResumeRating.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResumeController : ControllerBase
{
    private readonly IEvaluationService _evaluationService;

    public ResumeController(IEvaluationService evaluationService)
    {
        _evaluationService = evaluationService;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB max
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] string? linkedInUrl = null, [FromForm] string? gitHubUsername = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var allowedExtensions = new[] { ".pdf", ".docx", ".txt" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest("Only PDF, DOCX, and TXT files are supported.");

        using var stream = file.OpenReadStream();
        var resume = await _evaluationService.UploadResumeAsync(stream, file.FileName, linkedInUrl, gitHubUsername);
        return Ok(resume);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var resumes = await _evaluationService.GetResumesAsync();
        // Don't return extracted text in list view
        var summaries = resumes.Select(r => new
        {
            r.Id,
            r.FileName,
            r.CandidateName,
            r.LinkedInUrl,
            r.GitHubUsername,
            r.UploadedAt
        });
        return Ok(summaries);
    }

    [HttpDelete("{resumeId}")]
    public async Task<IActionResult> Delete(string resumeId)
    {
        try
        {
            await _evaluationService.DeleteResumeAsync(resumeId);
            return Ok(new { message = "Resume and associated evaluations deleted." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
