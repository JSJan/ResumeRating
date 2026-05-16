# Architecture Document — ResumeRating v2

## System Overview

```
┌─────────────────┐       HTTP/REST        ┌──────────────────────────────┐
│  React SPA       │◄─────────────────────►│  .NET 9 Web API               │
│  (localhost:3000) │       + SSE           │  (localhost:5073)             │
└─────────────────┘                        │                              │
                                           │  ┌──────────────────────┐    │
                                           │  │ Controllers          │    │
                                           │  │  ├─ Resume           │    │
                                           │  │  ├─ JobDescription   │    │
                                           │  │  └─ Evaluation       │    │
                                           │  └──────────┬───────────┘    │
                                           │             │                │
                                           │  ┌──────────▼───────────┐    │
                                           │  │ Services             │    │
                                           │  │  ├─ EvaluationSvc    │    │
                                           │  │  ├─ ResumeParserSvc  │    │
                                           │  │  ├─ ResumeAnalysisSvc│    │
                                           │  │  ├─ GitHubProfileSvc │    │
                                           │  │  └─ AI Service       │    │
                                           │  └──────────┬───────────┘    │
                                           │             │                │
                                           │  ┌──────────▼───────────┐    │
                                           │  │ Data Layer           │    │
                                           │  │  ├─ EF Core DbContext│    │
                                           │  │  └─ File Storage     │    │
                                           │  └──────────┬───────────┘    │
                                           └─────────────┼────────────────┘
                                                         │
                              ┌───────────────────────────┼────────────────────┐
                              │                           │                    │
                    ┌─────────▼──────┐        ┌───────────▼────┐    ┌─────────▼──────┐
                    │ SQLite DB       │        │ File System     │    │ External APIs   │
                    │ resumerating.db │        │ App_Data/       │    │  ├─ GitHub Models│
                    │                 │        │ Uploads/        │    │  ├─ Anthropic    │
                    │ • JobDescriptions│       │ • resume.pdf    │    │  └─ GitHub API   │
                    │ • Resumes        │       │ • resume.docx   │    └────────────────┘
                    │ • Evaluations    │       └────────────────┘
                    │ • Questionnaires │
                    │ • L1Feedback     │
                    │ • L2Assessments  │
                    │ • TokenUsageLogs │
                    └─────────────────┘
```

---

## Layer Architecture

### 1. Presentation Layer (React SPA)

| Component | Technology | Purpose |
|-----------|-----------|---------|
| Frontend | React 18 + TypeScript | Single-page application with 8 tabs |
| HTTP Client | Axios | REST API calls with 120s timeout |
| Streaming | Fetch API + SSE | Real-time evaluation progress |
| Styling | Inline CSS | Responsive grid layouts |

**Tabs:** Upload → Job Description → Evaluate → Compare → L1 Questions → L1 Feedback → L2 Round → L2 Result

### 2. API Layer (.NET 9 Controllers)

| Controller | Routes | Responsibility |
|------------|--------|---------------|
| `ResumeController` | `/api/resume/*` | Upload (single + batch), list, delete resumes |
| `JobDescriptionController` | `/api/jobdescription/*` | CRUD for job descriptions |
| `EvaluationController` | `/api/evaluation/*` | Evaluate, compare, L1/L2 rounds, token tracking |

### 3. Service Layer (Business Logic)

| Service | Lifetime | Responsibility |
|---------|----------|---------------|
| `EvaluationService` | Scoped | Orchestrates all workflows: upload, evaluate, L1, L2, seed |
| `ResumeParserService` | Singleton | Extracts text from PDF (iText7), DOCX (OpenXml), TXT |
| `ResumeAnalysisService` | Singleton | Local NLP: keyword matching, readability, AI detection heuristics |
| `GitHubModelsAiService` | Singleton | LLM calls via OpenAI SDK to GitHub Models endpoint |
| `AnthropicAiService` | Singleton | Fallback AI provider using Anthropic Claude |
| `GitHubProfileService` | Singleton | GitHub API: profile, repos, code samples with 24h cache |

### 4. Data Layer

| Component | Technology | Purpose |
|-----------|-----------|---------|
| `ResumeRatingDbContext` | EF Core + SQLite | ORM for all entities with relationships & cascade deletes |
| File Storage | Disk (`App_Data/Uploads/`) | Raw resume file storage (PDF, DOCX, TXT) |

---

## Database Architecture

### Entity-Relationship Diagram

```
┌──────────────────┐       1:N       ┌──────────────────────┐
│ JobDescriptions   │───────────────►│ CandidateEvaluations  │
│                   │                │                       │
│ PK: Id            │                │ PK: Id                │
│ Title             │                │ FK: JobDescriptionId  │
│ Description       │                │ FK: ResumeId          │
│ RequiredSkills    │                │ 14 Scores + Feedback  │
│ PreferredSkills   │                │ RecommendedForL1      │
│ ExperienceLevel   │                └───────────┬───────────┘
│ CreatedAt         │                            │
└──────────────────┘                  ┌──────────┼──────────┐
                                      │ 1:N      │ 1:N      │ 1:N
┌──────────────────┐           ┌──────▼─────┐ ┌──▼───────┐ ┌▼──────────┐
│ Resumes           │           │Questionnaire│ │L1Feedback│ │L2 entities│
│                   │───────────│             │ │          │ │           │
│ PK: Id            │   1:N     │ PK: Id      │ │ PK: Id   │ │ PK: Id    │
│ FileName          │           │ FK: EvalId  │ │ FK: EvalId│ │ FK: EvalId│
│ CandidateName     │           │ Questions[] │ │ Scores   │ │ Challenges│
│ ExtractedText     │           └─────────────┘ └──────────┘ └───────────┘
│ LinkedInUrl       │
│ GitHubUsername    │
│ GitHubProfileSum  │
│ UploadedAt        │
└──────────────────┘
```

### Cascade Delete Rules

| When Deleted | Also Deleted |
|-------------|-------------|
| JobDescription | All Evaluations for that JD |
| Resume | All Evaluations for that Resume |
| Evaluation | Questionnaires, L1Feedback, L2Questionnaires, L2Assessments |

### SQLite Configuration

```
Database file: App_Data/resumerating.db
Connection string: "Data Source=App_Data/resumerating.db"
Auto-migration: EnsureCreated on startup
WAL mode: Enabled for better concurrent read performance
```

---

## Data Flow: Resume Upload (Batch)

```
Client                    API                       Services                    Storage
  │                        │                           │                          │
  │─── POST /upload-multiple ──►│                      │                          │
  │    (files[], options)  │                           │                          │
  │                        │── foreach file ──────────►│                          │
  │                        │                           │── SaveFileAsync ────────►│ disk
  │                        │                           │── ExtractTextAsync ─────►│
  │                        │                           │── FetchGitHubProfile ───►│ (GitHub API)
  │                        │                           │── DbContext.Add ────────►│ SQLite
  │                        │                           │── SaveChangesAsync ─────►│
  │                        │◄── results[] ────────────│                          │
  │◄── { successful, failed } ─│                       │                          │
```

---

## Data Flow: Evaluation

```
Client                    API                  EvaluationSvc          AI Service        DB
  │                        │                       │                     │              │
  │── POST /evaluate ─────►│                       │                     │              │
  │                        │── EvaluateAsync ─────►│                     │              │
  │                        │                       │── Load Resume ─────────────────────►│
  │                        │                       │── Load JD ────────────────────────►│
  │                        │                       │── Check Existing ──────────────────►│
  │                        │                       │── LocalAnalysis ──►│               │
  │                        │                       │── GitHub Code ────►│ (GitHub API)  │
  │                        │                       │── EvaluateResume ──►│ (GPT-4o)     │
  │                        │                       │◄── 14 scores ──────│              │
  │                        │                       │── Save Eval ──────────────────────►│
  │                        │◄── evaluation ────────│                     │              │
  │◄── CandidateEvaluation │                       │                     │              │
```

---

## Dependency Injection Strategy

| Registration | Service | Reason |
|-------------|---------|--------|
| `AddDbContext<>` (Scoped) | `ResumeRatingDbContext` | One context per HTTP request, auto-disposed |
| Scoped | `EvaluationService` | Orchestrator with DB context dependency |
| Singleton | `ResumeParserService` | Stateless, thread-safe |
| Singleton | `ResumeAnalysisService` | Stateless NLP utilities |
| Singleton | `GitHubModelsAiService` | Shared HTTP client with token tracking |
| Singleton | `GitHubProfileService` | Shared cache (24h TTL) |

---

## AI Integration Architecture

### Model Selection

| Task | Model | Cost (per 1M tokens) |
|------|-------|---------------------|
| Resume Evaluation (14 scores) | gpt-4o | $2.50 in / $10.00 out |
| L1 Questionnaire Generation | gpt-4o-mini | $0.15 in / $0.60 out |
| L1 Answer Evaluation | gpt-4o | $2.50 in / $10.00 out |
| L2 Challenge Generation | gpt-4o | $2.50 in / $10.00 out |
| L2 Assessment | gpt-4o | $2.50 in / $10.00 out |

### Prompt Strategy
- Structured JSON output format enforced in system prompts
- Local pre-analysis (keyword matching, readability, AI detection heuristics) included to reduce token usage
- GitHub code samples injected for code proficiency scoring

---

## Security Considerations

| Area | Measure |
|------|---------|
| File Upload | Extension whitelist (.pdf, .docx, .txt), 10MB size limit |
| File Names | Sanitized via `Path.GetInvalidFileNameChars()` to prevent path traversal |
| Database | Parameterized queries via EF Core (SQL injection safe) |
| API Keys | Stored in user secrets / environment variables, never in source |
| CORS | Restricted to `http://localhost:3000` |

---

## Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=App_Data/resumerating.db"
  },
  "GitHub": {
    "Token": "<user-secret>",
    "Model": "gpt-4o",
    "LightModel": "gpt-4o-mini"
  },
  "Anthropic": {
    "ApiKey": "<user-secret>",
    "LightModel": ""
  },
  "Ai": {
    "TimeoutSeconds": 120
  }
}
```

---

## Technology Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Runtime | .NET | 9.0 |
| ORM | Entity Framework Core | 9.x |
| Database | SQLite | via Microsoft.EntityFrameworkCore.Sqlite |
| PDF Parsing | iText7 | 9.6.0 |
| DOCX Parsing | DocumentFormat.OpenXml | 3.5.1 |
| AI (Primary) | OpenAI SDK → GitHub Models | 2.10.0 |
| AI (Fallback) | Anthropic.SDK | 5.10.0 |
| JSON | Newtonsoft.Json | 13.0.4 |
| API Docs | Swashbuckle (Swagger) | 6.9.0 |
| Frontend | React + TypeScript | 18.3.1 |
| HTTP Client | Axios | 1.7.0 |
