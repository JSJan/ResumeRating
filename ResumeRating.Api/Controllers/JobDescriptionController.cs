using Microsoft.AspNetCore.Mvc;
using ResumeRating.Api.Models;
using ResumeRating.Api.Services;

namespace ResumeRating.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobDescriptionController : ControllerBase
{
    private readonly IEvaluationService _evaluationService;

    public JobDescriptionController(IEvaluationService evaluationService)
    {
        _evaluationService = evaluationService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] JobDescription jd)
    {
        if (string.IsNullOrWhiteSpace(jd.Title) || string.IsNullOrWhiteSpace(jd.Description))
            return BadRequest("Title and Description are required.");

        var created = await _evaluationService.CreateJobDescriptionAsync(jd);
        return Ok(created);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var jds = await _evaluationService.GetJobDescriptionsAsync();
        return Ok(jds);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            await _evaluationService.DeleteJobDescriptionAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
