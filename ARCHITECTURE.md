# Architecture

## System Overview

The Resume Rating System is a two-tier application with a .NET Web API backend and a React SPA frontend, using Anthropic Claude as the AI engine.

```
┌─────────────────────┐     HTTP/REST     ┌──────────────────────────┐
│                     │  ◄──────────────► │                          │
│   React Frontend    │    localhost:3000  │   .NET 9 Web API         │
│   (TypeScript)      │                   │   localhost:5073          │
│                     │                   │                          │
└─────────────────────┘                   └──────┬───────┬───────────┘
                                                 │       │
                                          ┌──────┘       └──────┐
                                          ▼                      ▼
                                   ┌─────────────┐      ┌──────────────┐
                                   │  GIthub azure│      │  File-based  │
                                   │             │      │  JSON Store  │
                                   └─────────────┘      └──────────────┘
```

## Project Structure

```
ResumeRating/
├── ResumeRating.Api/              # Backend
│   ├── Controllers/
│   │   ├── ResumeController       # Upload + list resumes
│   │   ├── JobDescriptionController # CRUD job descriptions
│   │   └── EvaluationController   # Evaluate, questionnaire, L1 feedback, seed
│   ├── Models/
│   │   ├── Resume                 # Uploaded resume with extracted text
│   │   ├── JobDescription         # JD with skills and experience level
│   │   ├── CandidateEvaluation    # 7-dimension scoring + salary estimates
│   │   ├── Questionnaire          # Generated L1 interview questions
│   │   └── L1Feedback             # Answer evaluations + tech round recommendation
│   ├── Services/
│   │   ├── IAiService             # AI provider abstraction
│   │   ├── AnthropicAiService     # Claude API integration
│   │   ├── IEvaluationService     # Orchestrates the evaluation workflow
│   │   ├── EvaluationService      # Full implementation including seed
│   │   ├── IResumeParserService   # Text extraction abstraction
│   │   ├── ResumeParserService    # PDF/DOCX/TXT parsing
│   │   ├── IStorageService        # Persistence abstraction
│   │   └── FileStorageService     # JSON file storage
│   ├── Program.cs                 # DI setup, middleware, CORS
│   └── appsettings.json           # Configuration
├── resume-rating-ui/              # Frontend
│   └── src/
│       ├── App.tsx                # Single-page app with 5 tabs
│       ├── api.ts                 # Axios client + TypeScript interfaces
│       └── index.tsx              # React entry point
├── assets/                        # Sample data
│   ├── *.pdf                      # 7 sample resumes
│   └── job-description.md         # Sample JD (parsed by seed endpoint)
└── Prompts/                       # Project requirements
```

## Key Design Decisions

### 1. AI Provider Abstraction (`IAiService`)

The AI logic is behind the `IAiService` interface, making it easy to swap providers:

```csharp
public interface IAiService
{
    Task<CandidateEvaluation> EvaluateResumeAsync(string resumeText, JobDescription jd);
    Task<Questionnaire> GenerateQuestionnaireAsync(CandidateEvaluation eval, string resumeText, JobDescription jd);
    Task<L1Feedback> EvaluateL1AnswersAsync(CandidateEvaluation eval, Questionnaire q, List<CandidateAnswer> answers, string resumeText, JobDescription jd);
}
```

Currently implemented by `AnthropicAiService` (Claude). To add OpenAI or local models, implement this interface and swap the DI registration in `Program.cs`.

### 2. Structured JSON Prompts

All AI prompts request responses in strict JSON format. The service strips markdown code fences and deserializes into strongly-typed C# models. This ensures:
- Predictable parsing
- Type-safe access to scores and feedback
- No regex or text extraction hacks

### 3. File-based Storage

Data is stored as JSON files in `App_Data/` instead of a database:
- **Zero setup** — no database server needed
- **Inspectable** — open the JSON files to see data
- **Resettable** — delete the folder to start fresh

Trade-off: Not suitable for concurrent access or production scale. The `IStorageService` interface allows swapping to a real database later.

### 4. Single-file Frontend

The React frontend is intentionally kept in a single `App.tsx` file with inline styles:
- **Low ceremony** — no component library, CSS framework, or router
- **Easy to read** — entire UI in one place
- **Fast to iterate** — no build configuration to manage

### 5. Resume Parsing Pipeline

```
PDF  ──► iText7 ──────────────────────────────►
DOCX ──► OpenXml (DocumentFormat.OpenXml) ─────► Extracted Text ──► AI Evaluation
TXT  ──► StreamReader ────────────────────────►
```

Text extraction happens at upload time and is stored with the resume record. This avoids re-parsing on every evaluation.

## Data Flow

### Evaluation Flow

```
1. Upload Resume ──► Parse text ──► Store resume + text
2. Create JD ──► Store JD
3. Evaluate ──► Send (resume text + JD) to Claude ──► Get 7 scores + feedback + salary ──► Store evaluation
4. Generate Questionnaire ──► Send (evaluation + resume + JD) to Claude ──► Get 10-15 questions ──► Store
5. Submit Answers ──► Send (questions + answers + context) to Claude ──► Get L1 feedback ──► Store
```

### Scoring Dimensions

| Dimension | What it measures |
|-----------|-----------------|
| Experience | Years and depth of relevant experience |
| Work History | Quality of companies and roles |
| Education | Academic background relevance |
| Side Projects | Open source, blogs, personal projects |
| Job Fit | Alignment with JD requirements |
| Aww Factor | What makes them impressive |
| Uniqueness | What sets them apart from other candidates |

## Dependency Injection

```
Singleton:  FileStorageService, ResumeParserService, AnthropicAiService
Scoped:     EvaluationService (per-request orchestrator)
```

Storage and parser are singletons (stateless). The AI service is singleton (holds the API client). EvaluationService is scoped since it orchestrates per-request workflows.
