using ResumeRating.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IStorageService, FileStorageService>();
builder.Services.AddSingleton<IResumeParserService, ResumeParserService>();
builder.Services.AddSingleton<IResumeAnalysisService, ResumeAnalysisService>();
builder.Services.AddSingleton<IAiService, GitHubModelsAiService>();
builder.Services.AddSingleton<IGitHubProfileService, GitHubProfileService>();
builder.Services.AddScoped<IEvaluationService, EvaluationService>();

// CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Resume Rating API", Version = "v1", Description = "AI-powered resume evaluation and interview preparation system" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Resume Rating API v1"));
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
