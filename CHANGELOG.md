# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [0.5.0] - 2026-04-18

### Added
- **Runtime token tracking** — `GET /api/evaluation/token-usage` returns per-call token counts, input/output breakdown, estimated cost, and summary grouped by operation and model
- `GET /api/evaluation/token-usage/log` for full call-by-call history
- **Claude Haiku integration** — set `Anthropic:ApiKey` + `Anthropic:LightModel` in appsettings.json to route L1/L2 calls through Anthropic Haiku instead of GPT-4o-mini
- **Confirmation dialogs** before all AI-powered operations (Evaluate, Evaluate All, Generate L1/L2 Questionnaire, Submit L1/L2 Answers) to prevent accidental token usage
- **Dimension tooltips** — hover over any score label in the evaluation view to see what that dimension measures
- **Contextual error messages** — network errors, timeouts, 404s, 400s, and 500s now provide actionable guidance instead of generic text

### Changed
- **Tab bar** — responsive horizontal scroll with emoji icons; no longer wraps/overflows on smaller screens
- `GitHub:LightModel` setting now documented in appsettings.json
- `appsettings.json` includes `Anthropic:LightModel` field for hybrid provider configuration

### Fixed
- Tab bar overflow on screens narrower than 960px (8 tabs previously wrapped to multiple rows)
- Generic "An error occurred" messages replaced with specific guidance per error type

## [0.4.0] - 2026-04-17

### Added
- **Cost optimization pipeline** — `ResumeAnalysisService` performs local pre-processing (keyword match, readability, AI-detection, years of experience, education level, buzzword counting, power verb detection, GitHub skill overlap) before AI evaluation
- **Multi-model support** — GPT-4o for evaluations, GPT-4o-mini for L1/L2 questionnaires and answer evaluation (~93% cheaper)
- **GitHub data caching** — 24-hour in-memory cache for profile data and code analysis
- **Duplicate evaluation prevention** — `EvaluateAsync` returns cached result when resume+JD combo already evaluated
- **Prompt optimization** — consolidated 3 overlapping sections into 1, compact JSON schema, conditional sections, resume text truncation at 8k chars
- **OpenAPI spec** — `ResumeRating.Api/docs/openapi.yaml` covering all 14 endpoints
- TOKEN_USAGE_COST.md, TEST_ARCHITECTURE.md, API_PERFORMANCE.md documentation

### Fixed
- Individual evaluate creating duplicate evaluations for same resume+JD combo

## [0.3.0] - 2026-04-15

### Added
- **Candidate Comparison tab** — side-by-side ranking of all evaluated candidates for a JD with sortable columns, summary stats (total, recommended, avg score, top scorer), and candidate highlights
- Configurable AI timeout via `Ai:TimeoutSeconds` in `appsettings.json` (default: 120 seconds)
- Axios 2-minute request timeout on the frontend to match backend

### Changed
- Updated README to reference GitHub Models (GPT-4o) instead of Anthropic Claude
- Frontend tabs now include "Compare All" between Evaluate and L1 Questionnaire

## [0.2.0] - 2026-04-15

### Changed
- Switched AI provider from OpenAI to **Anthropic Claude** (claude-sonnet-4-20250514)
- Replaced `OpenAI` NuGet package with `Anthropic.SDK`
- Renamed `IOpenAiService` to `IAiService` for provider-agnostic interface

### Added
- Seed endpoint (`POST /api/evaluation/seed`) to auto-load resumes from `assets/` folder
- Job description markdown file (`assets/job-description.md`) for sample data
- "Load Sample Data" button in the frontend UI
- CHANGELOG.md
- LOCAL_SETUP.md
- CONTRIBUTING.md
- ARCHITECTURE.md

### Fixed
- `FileStorageService` crash when `Storage:DataPath` config is empty string

## [0.1.0] - 2026-04-15

### Added
- Initial project scaffold with .NET 9 Web API backend
- React + TypeScript frontend with tabbed UI
- Resume parsing for PDF (iText7), DOCX (OpenXml), and TXT
- AI-powered resume evaluation with 7-dimensional scoring
- Salary and role estimation
- L1 interview questionnaire generation
- L1 answer evaluation with tech round recommendations
- File-based JSON storage
- CORS configuration for local development
