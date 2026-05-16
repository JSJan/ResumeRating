# Product Requirements Document (PRD)

## ResumeRating — AI-Powered Resume Evaluation & Interview System

**Version:** 2.0  
**Date:** May 2026  
**Status:** In Development

---

## 1. Executive Summary

ResumeRating is an AI-powered recruitment assistant that automates resume screening, candidate evaluation, and multi-stage interview preparation. It ingests job descriptions, parses uploaded resumes, and uses LLM-based evaluation across 14 scoring dimensions to produce objective, data-driven candidate rankings.

This PRD covers the transition from file-based storage to a relational database (SQLite) and the addition of bulk resume upload capabilities.

---

## 2. Problem Statement

Hiring managers spend **23 hours per hire** screening resumes manually. Current ATS tools rely on keyword matching, missing nuanced signals like resume authenticity, AI-generated content detection, and GitHub code proficiency. ResumeRating solves this with deep AI analysis while keeping the hiring team in control through structured L1/L2 interview rounds.

### Current Limitations (v1)
- **File-based JSON storage** — no ACID guarantees, no concurrent access safety, no querying capability
- **Single resume upload** — users must upload resumes one at a time
- **No data durability** — accidental file deletion loses all data

---

## 3. Goals & Objectives

| Goal | Metric | Target |
|------|--------|--------|
| Migrate to database storage | Data integrity | ACID-compliant SQLite with EF Core |
| Bulk resume upload | Upload throughput | ≤10 files per batch, max 10MB each |
| Maintain zero-config local setup | Setup friction | `dotnet run` starts with auto-migration |
| Preserve all existing features | Feature parity | 100% backward compatibility |

---

## 4. User Personas

### Hiring Manager (Primary)
- Creates job descriptions, reviews candidate rankings
- Wants fast bulk upload and side-by-side comparison
- Needs persistent, queryable data

### Technical Interviewer (Secondary)
- Reviews L1/L2 questionnaires and candidate responses
- Relies on AI-generated scoring and feedback
- Needs evaluation history across multiple JDs

### Recruiter (Tertiary)
- Uploads batches of resumes from multiple sources
- Needs quick turnaround on initial screening
- Values bulk operations and progress tracking

---

## 5. Features & Requirements

### 5.1 Database Storage (SQLite + EF Core)

**Priority:** P0 — Critical

| Requirement | Description |
|------------|-------------|
| DB-001 | Use SQLite as the default database (zero infrastructure) |
| DB-002 | Use Entity Framework Core for ORM with code-first migrations |
| DB-003 | Store all entities: JobDescriptions, Resumes, Evaluations, Questionnaires, L1Feedback, L2Questionnaires, L2Assessments, TokenUsageLogs |
| DB-004 | Auto-create database on first run (EnsureCreated or auto-migration) |
| DB-005 | Connection string configurable via `appsettings.json` |
| DB-006 | Resume file uploads remain on disk (`App_Data/Uploads/`) |
| DB-007 | Cascade delete: deleting a JD removes its evaluations; deleting a resume removes its evaluations |

#### Database Schema

**JobDescriptions**
| Column | Type | Constraints |
|--------|------|------------|
| Id | TEXT (GUID) | PRIMARY KEY |
| Title | TEXT | NOT NULL |
| Description | TEXT | NOT NULL |
| RequiredSkills | TEXT | |
| PreferredSkills | TEXT | |
| ExperienceLevel | TEXT | |
| CreatedAt | DATETIME | NOT NULL, DEFAULT UTC_NOW |

**Resumes**
| Column | Type | Constraints |
|--------|------|------------|
| Id | TEXT (GUID) | PRIMARY KEY |
| FileName | TEXT | NOT NULL |
| CandidateName | TEXT | |
| ExtractedText | TEXT | |
| LinkedInUrl | TEXT | |
| GitHubUsername | TEXT | |
| GitHubProfileSummary | TEXT | |
| UploadedAt | DATETIME | NOT NULL |

**Evaluations**
| Column | Type | Constraints |
|--------|------|------------|
| Id | TEXT (GUID) | PRIMARY KEY |
| ResumeId | TEXT | FK → Resumes(Id) ON DELETE CASCADE |
| JobDescriptionId | TEXT | FK → JobDescriptions(Id) ON DELETE CASCADE |
| CandidateName | TEXT | |
| OverallScore | REAL | |
| ... (14 scores + feedback) | | |
| RecommendedForL1 | INTEGER (bool) | |
| EvaluatedAt | DATETIME | |

**Questionnaires, L1Feedback, L2Questionnaires, L2Assessments** — foreign keys to Evaluations with cascade delete.

### 5.2 Multiple Resume Upload

**Priority:** P0 — Critical

| Requirement | Description |
|------------|-------------|
| MR-001 | New API endpoint `POST /api/resume/upload-multiple` accepting multiple `IFormFile` |
| MR-002 | Accept up to 10 files per request |
| MR-003 | Each file validated independently (type, size) |
| MR-004 | Return per-file success/failure results |
| MR-005 | Optional LinkedIn URL and GitHub username per file (applied to all in batch) |
| MR-006 | Frontend drag-and-drop multi-file selector |
| MR-007 | Progress indicator showing per-file upload status |

#### API Contract

**Request:** `POST /api/resume/upload-multiple`
```
Content-Type: multipart/form-data

files: [file1.pdf, file2.docx, file3.txt]
linkedInUrl: (optional, applies to all)
gitHubUsername: (optional, applies to all)
```

**Response:**
```json
{
  "successful": [
    { "fileName": "file1.pdf", "resume": { "id": "...", "candidateName": "..." } }
  ],
  "failed": [
    { "fileName": "file3.txt", "error": "File is empty" }
  ],
  "totalUploaded": 2,
  "totalFailed": 1
}
```

### 5.3 Existing Features (Maintained)

| Feature | Description |
|---------|-------------|
| Resume Parsing | PDF (iText7), DOCX (OpenXml), TXT extraction |
| 14-Dimension Evaluation | Experience, WorkHistory, Education, SideProjects, JobFit, AwwFactor, Uniqueness, GitHub, OnlinePresence, CodeProficiency, ResumeAuthenticity, Buzzword, AIGenerated, Overall |
| L1 Interview | AI-generated questionnaire (10-15 questions), answer evaluation, tech round recommendation |
| L2 Technical Round | 4 challenges: System Design, Hands-On Coding, Design Thinking, Trade-Off Analysis |
| GitHub Deep Analysis | Profile summary, repo code samples, language/skill matching |
| Resume Authenticity | Tailoring red flags, AI-generated content detection, buzzword analysis |
| Candidate Comparison | Side-by-side ranking with sortable columns |
| Token Usage Tracking | Per-operation cost tracking with model-level pricing |
| SSE Streaming | Real-time progress updates during evaluation |
| Seed Data | Load sample resumes and JD from `assets/` folder |

---

## 6. Non-Functional Requirements

| Category | Requirement |
|----------|------------|
| Performance | Resume upload < 2s per file; DB queries < 100ms |
| Scalability | SQLite supports up to ~100 concurrent reads; sufficient for single-team use |
| Security | File type validation, size limits (10MB), filename sanitization, parameterized queries via EF Core |
| Reliability | ACID transactions for all writes; auto-migration on startup |
| Compatibility | .NET 9, React 18, macOS/Windows/Linux |
| Data Portability | SQLite file is a single portable file (`App_Data/resumerating.db`) |

---

## 7. Technical Constraints

- **AI Provider**: GitHub Models (GPT-4o primary, GPT-4o-mini for light tasks) with Anthropic Claude fallback
- **File Storage**: Resume files (PDF/DOCX/TXT) stored on local disk, metadata in DB
- **No External DB**: SQLite chosen for zero-infrastructure local development
- **Single Instance**: No horizontal scaling requirements (local tool)

---

## 8. Out of Scope (v2)

- Multi-tenant / cloud deployment
- User authentication & authorization
- Email notifications
- Calendar integration for interview scheduling
- PostgreSQL / SQL Server migration (future v3)
- Resume deduplication by content hash

---

## 9. Success Criteria

1. All existing API endpoints work identically after DB migration
2. Bulk upload of 10 resumes completes within 20 seconds
3. Database auto-creates on first `dotnet run`
4. Deleting a JD cascades to all related evaluations
5. Frontend supports multi-file selection and shows per-file status

---

## 10. Timeline

| Phase | Deliverable | Status |
|-------|------------|--------|
| Phase 1 | PRD & Architecture docs | ✅ Complete |
| Phase 2 | SQLite + EF Core integration | ✅ Complete |
| Phase 3 | Multi-resume upload API + UI | ✅ Complete |
| Phase 4 | Testing & validation | Pending |
