# API Performance — Time Taken Per API Call

## Overview

This document profiles the expected time taken for each API endpoint, broken down by internal operations (I/O, parsing, network calls, AI inference).

---

## Performance Profile by Endpoint

### 1. `POST /api/resume/upload` — Upload Resume

| Step | Operation | Expected Time |
|------|-----------|---------------|
| 1 | Receive multipart file upload | 50–500ms (depends on file size) |
| 2 | Save file to disk | 10–50ms |
| 3 | Re-open and extract text (PDF/DOCX/TXT) | 100–2,000ms |
| 4 | Fetch GitHub profile (if username provided) | 500–3,000ms |
| 5 | Save resume to JSON storage | 10–50ms |
| **Total** | | **~200ms–5,500ms** |

**Notes:**

- PDF parsing with iText is the slowest local step (~100–2,000ms for large PDFs)
- GitHub API calls add ~1–3s of network latency
- Without GitHub: ~200–2,500ms

---

### 2. `GET /api/resume` — List Resumes

| Step | Operation | Expected Time |
|------|-----------|---------------|
| 1 | Read resumes.json from disk | 5–50ms |
| 2 | Deserialize JSON | 1–10ms |
| 3 | Project to summary (strip ExtractedText) | <1ms |
| **Total** | | **~10–60ms** |

---

### 3. `DELETE /api/resume/{resumeId}` — Delete Resume

| Step | Operation | Expected Time |
|------|-----------|---------------|
| 1 | Load resumes.json | 5–50ms |
| 2 | Remove matching resume | <1ms |
| 3 | Save resumes.json | 5–50ms |
| 4 | Load evaluations.json | 5–50ms |
| 5 | Remove associated evaluations | <1ms |
| 6 | Save evaluations.json | 5–50ms |
| **Total** | | **~25–200ms** |

---

### 4. `POST /api/jobdescription` — Create Job Description

| Step | Operation | Expected Time |
|------|-----------|---------------|
| 1 | Generate GUID | <1ms |
| 2 | Append to job_descriptions.json | 10–50ms |
| **Total** | | **~10–50ms** |

---

### 5. `GET /api/jobdescription` — List Job Descriptions

| Step | Operation | Expected Time |
|------|-----------|---------------|
| 1 | Read job_descriptions.json | 5–50ms |
| 2 | Deserialize JSON | 1–5ms |
| **Total** | | **~5–55ms** |

---

### 6. `POST /api/evaluation/evaluate` — Single Candidate Evaluation ⚡

| Step | Operation | Expected Time |
|------|-----------|---------------|
| 1 | Load resumes.json | 5–50ms |
| 2 | Load job_descriptions.json | 5–50ms |
| 3 | Load evaluations.json (duplicate check) | 5–50ms |
| 4 | If already evaluated: return cached | **~20–150ms (fast path)** |
| 5 | Refresh GitHub profile (if needed) | 500–3,000ms |
| 6 | Analyze GitHub code (fetch repos + code) | 2,000–10,000ms |
| 7 | **AI evaluation call (GPT-4o)** | **15,000–60,000ms** |
| 8 | Deserialize AI response | 1–5ms |
| 9 | Save evaluation to JSON | 10–50ms |
| **Total (new eval)** | | **~18s–73s** |
| **Total (cached)** | | **~20–150ms** |

**Notes:**

- AI inference dominates at 15–60s depending on prompt size and model load
- GitHub code analysis adds 2–10s of network calls
- The duplicate check (newly added) makes repeated evaluations instant

---

### 7. `POST /api/evaluation/evaluate-stream` — SSE Streaming Evaluation

Same timing as single evaluation, but progress events are emitted:

| Event | Approx. Time Since Start |
|-------|-------------------------|
| `start` | 0ms |
| `loading` | 50–100ms |
| `linkedin` | 100–200ms |
| `github_profile` | 200–3,000ms |
| `github_code` | 3,000–10,000ms |
| `ai_eval` (start) | ~10,000ms |
| `ai_eval` (buzzword) | ~10,500ms |
| `ai_eval` (scoring) | ~11,000ms |
| `complete` | 18,000–73,000ms |
| `result` | 18,100–73,100ms |

---

### 8. `POST /api/evaluation/evaluate-all/{jobDescriptionId}` — Bulk Evaluation ⚡⚡

| Step | Operation | Expected Time |
|------|-----------|---------------|
| 1 | Load JDs, resumes, existing evaluations | 20–150ms |
| 2 | Filter out already-evaluated resumes | <1ms |
| 3 | Per-resume evaluation (sequential) | 18–73s × N resumes |
| 4 | Reload all evaluations for JD | 10–50ms |
| **Total (N new resumes)** | | **~18s × N to ~73s × N** |

**Example:**

- 5 new candidates: ~90s–365s (1.5–6 min)
- 10 new candidates: ~180s–730s (3–12 min)

**Optimization opportunity:** Parallel evaluation could reduce this significantly.

---

### 9. `GET /api/evaluation/{jobDescriptionId}` — Get Evaluations

| Step | Operation | Expected Time |
|------|-----------|---------------|
| 1 | Load evaluations.json | 5–100ms (grows with data) |
| 2 | Filter by JD ID | <1ms |
| **Total** | | **~5–100ms** |

---

### 10. `DELETE /api/evaluation/{evaluationId}` — Delete Evaluation

| Step | Operation | Expected Time |
|------|-----------|---------------|
| 1 | Load evaluations.json | 5–100ms |
| 2 | Remove matching evaluation | <1ms |
| 3 | Save evaluations.json | 5–100ms |
| **Total** | | **~15–200ms** |

---

### 11. `POST /api/evaluation/questionnaire/{evaluationId}` — Generate L1 Questionnaire

| Step | Operation | Expected Time |
|------|-----------|---------------|
| 1 | Load evaluation + resume + JD | 20–150ms |
| 2 | Validate L1 recommendation | <1ms |
| 3 | **AI questionnaire generation** | **10,000–30,000ms** |
| 4 | Save questionnaire | 10–50ms |
| **Total** | | **~10s–30s** |

---

### 12. `POST /api/evaluation/l1-feedback` — Submit L1 Answers

| Step | Operation | Expected Time |
|------|-----------|---------------|
| 1 | Load evaluation + questionnaire + resume + JD | 30–200ms |
| 2 | Build Q&A section | <5ms |
| 3 | **AI answer evaluation** | **10,000–30,000ms** |
| 4 | Save L1 feedback | 10–50ms |
| **Total** | | **~10s–30s** |

---

### 13. `POST /api/evaluation/l2-questionnaire/{evaluationId}` — Generate L2 Questionnaire

| Step | Operation | Expected Time |
|------|-----------|---------------|
| 1 | Load evaluation + L1 feedback + resume + JD | 30–200ms |
| 2 | Validate tech round recommendation | <1ms |
| 3 | **AI L2 challenge generation** | **10,000–30,000ms** |
| 4 | Save L2 questionnaire | 10–50ms |
| **Total** | | **~10s–30s** |

---

### 14. `POST /api/evaluation/l2-feedback` — Submit L2 Answers

| Step | Operation | Expected Time |
|------|-----------|---------------|
| 1 | Load evaluation + L2 questionnaire + resume + JD | 30–200ms |
| 2 | **AI L2 assessment** | **10,000–30,000ms** |
| 3 | Save L2 assessment | 10–50ms |
| **Total** | | **~10s–30s** |

---

### 15. `POST /api/evaluation/seed` — Seed from Assets

| Step | Operation | Expected Time |
|------|-----------|---------------|
| 1 | Read assets directory | 5–10ms |
| 2 | Parse JD markdown | 1–5ms |
| 3 | Check/create JD | 10–50ms |
| 4 | Per resume: read file + extract text + save | 100–2,000ms × N |
| **Total (5 resumes)** | | **~500ms–10s** |

---

## Performance Summary

| Endpoint | Method | Avg Time | Bottleneck |
|----------|--------|----------|------------|
| `/api/resume` | GET | ~30ms | Disk I/O |
| `/api/jobdescription` | GET | ~30ms | Disk I/O |
| `/api/jobdescription` | POST | ~30ms | Disk I/O |
| `/api/evaluation/{jdId}` | GET | ~50ms | Disk I/O |
| `/api/evaluation/{id}` | DELETE | ~100ms | Disk I/O |
| `/api/resume/{id}` | DELETE | ~120ms | Disk I/O (2 files) |
| `/api/resume/upload` | POST | ~1–5s | PDF parsing + GitHub |
| `/api/evaluation/seed` | POST | ~2–10s | PDF parsing × N |
| `/api/evaluation/questionnaire/{id}` | POST | ~10–30s | **AI inference** |
| `/api/evaluation/l1-feedback` | POST | ~10–30s | **AI inference** |
| `/api/evaluation/l2-questionnaire/{id}` | POST | ~10–30s | **AI inference** |
| `/api/evaluation/l2-feedback` | POST | ~10–30s | **AI inference** |
| `/api/evaluation/evaluate` | POST | ~18–73s | **AI + GitHub** |
| `/api/evaluation/evaluate-all/{jdId}` | POST | ~18–73s × N | **AI × N (sequential)** |

---

## Performance Optimization Recommendations

### Quick Wins

1. **Duplicate evaluation check** — Already implemented; returns cached result in ~100ms
2. **GitHub data caching** — Cache profile data for 24h to avoid re-fetching
3. **Async file I/O** — Already using async; ensure no blocking calls

### Medium Effort

4. **Parallel evaluate-all** — Use `Task.WhenAll` with concurrency limit (e.g., 3 at a time) to reduce bulk evaluation time by 3×
2. **Smaller model for non-eval calls** — Use GPT-4o-mini for questionnaire generation (~50% faster)
3. **Pre-extract resume text at upload** — Already done; no change needed

### Higher Effort

7. **Move to database** — Replace JSON file I/O with SQLite or CosmosDB for faster reads under load
2. **Background job queue** — Offload AI calls to a job queue (Hangfire/Azure Queue) for better UX
3. **Response caching** — Cache GET endpoints with ETags for repeated requests

### Infrastructure

10. **CDN for static assets** — If hosting the React UI
2. **Connection pooling** — For GitHub API HttpClient (already singleton)
3. **Timeout tuning** — Current 120s timeout is generous; consider 90s with retry
