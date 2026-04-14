using Microsoft.AspNetCore.Mvc;
using ResumeRating.Api.Models;
using ResumeRating.Api.Services;

namespace ResumeRating.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EvaluationController : ControllerBase
{
    private readonly IEvaluationService _evaluationService;

    public EvaluationController(IEvaluationService evaluationService)
    {
        _evaluationService = evaluationService;
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

    [HttpGet("{jobDescriptionId}")]
    public async Task<IActionResult> GetEvaluations(string jobDescriptionId)
    {
        var evaluations = await _evaluationService.GetEvaluationsAsync(jobDescriptionId);
        return Ok(evaluations);
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

    public class EvaluateRequest
    {
        public string ResumeId { get; set; } = string.Empty;
        public string JobDescriptionId { get; set; } = string.Empty;
    }
}
