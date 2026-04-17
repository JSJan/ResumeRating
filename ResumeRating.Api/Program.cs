using ResumeRating.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IStorageService, FileStorageService>();
builder.Services.AddSingleton<IResumeParserService, ResumeParserService>();
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
