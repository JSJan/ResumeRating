# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

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
