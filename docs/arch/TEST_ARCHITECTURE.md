# Test Architecture — Resume Rating Application

## Overview

This document defines the test strategy, test cases, and quality assurance plan for the Resume Rating application from a **Test Architect** perspective.

---

## Test Pyramid

```
                    ┌─────────┐
                    │  E2E    │  ← 5–10 critical user journeys
                   ┌┴─────────┴┐
                   │Integration │  ← API + service layer tests
                  ┌┴───────────┴┐
                  │  Unit Tests  │  ← Models, parsers, storage, logic
                 ┌┴─────────────┴┐
                 │  Static/Lint   │  ← Build, analyzers, code style
                 └───────────────┘
```

---

## 1. Unit Tests

### 1.1 Resume Parser Service

| Test ID | Test Case | Input | Expected | Priority |
|---------|-----------|-------|----------|----------|
| UP-01 | Parse valid PDF | Sample PDF stream | Non-empty extracted text | P0 |
| UP-02 | Parse valid DOCX | Sample DOCX stream | Non-empty extracted text | P0 |
| UP-03 | Parse valid TXT | Sample TXT stream | Exact text content | P0 |
| UP-04 | Reject unsupported format (.doc) | .doc stream | `NotSupportedException` | P0 |
| UP-05 | Reject unknown extension | .xyz stream | `NotSupportedException` | P1 |
| UP-06 | Handle empty PDF | Empty PDF stream | Empty string or graceful result | P1 |
| UP-07 | Handle corrupted PDF | Corrupted bytes | Exception (not crash) | P1 |
| UP-08 | Large file (10MB) | 10MB PDF | Completes within 10s | P2 |

### 1.2 Storage Service

| Test ID | Test Case | Expected | Priority |
|---------|-----------|----------|----------|
| ST-01 | Save and load single object | Round-trip preserves data | P0 |
| ST-02 | Append to list | Item added, existing items preserved | P0 |
| ST-03 | Load empty/missing collection | Returns empty list | P0 |
| ST-04 | Save and load list | All items preserved | P0 |
| ST-05 | Save file to uploads | File exists at expected path | P0 |
| ST-06 | Sanitize filenames with special chars | No invalid path characters | P1 |
| ST-07 | Concurrent writes don't corrupt data | Data integrity under parallel access | P1 |
| ST-08 | GetUploadPath returns correct path | Matches expected path pattern | P2 |

### 1.3 Evaluation Service — Business Logic

| Test ID | Test Case | Expected | Priority |
|---------|-----------|----------|----------|
| ES-01 | EvaluateAsync — resume not found | `KeyNotFoundException` | P0 |
| ES-02 | EvaluateAsync — JD not found | `KeyNotFoundException` | P0 |
| ES-03 | EvaluateAsync — duplicate prevention | Returns existing evaluation, no duplicate | P0 |
| ES-04 | EvaluateAllAsync — skips already evaluated | Only new resumes evaluated | P0 |
| ES-05 | DeleteResumeAsync — cascades to evaluations | Resume + evaluations removed | P0 |
| ES-06 | DeleteResumeAsync — not found | `KeyNotFoundException` | P1 |
| ES-07 | DeleteEvaluationAsync — not found | `KeyNotFoundException` | P1 |
| ES-08 | GenerateQuestionnaireAsync — not recommended for L1 | `InvalidOperationException` | P0 |
| ES-09 | GenerateL2QuestionnaireAsync — not recommended for tech | `InvalidOperationException` | P0 |
| ES-10 | UploadResumeAsync — saves file + extracts text | Resume object with extracted text | P0 |
| ES-11 | UploadResumeAsync — with GitHub username | GitHub profile fetched | P1 |
| ES-12 | UploadResumeAsync — GitHub fetch fails gracefully | Resume saved without GitHub data | P1 |
| ES-13 | SeedFromAssetsAsync — skips duplicate resumes | No duplicate filenames | P0 |
| ES-14 | SeedFromAssetsAsync — skips duplicate JD titles | No duplicate JD | P0 |
| ES-15 | SeedFromAssetsAsync — assets folder missing | `DirectoryNotFoundException` | P1 |
| ES-16 | ExtractNameHeuristic | First line of text returned | P2 |
| ES-17 | ParseSkillsList | Comma/semicolon/pipe separated skills parsed | P2 |

### 1.4 GitHub Profile Service

| Test ID | Test Case | Expected | Priority |
|---------|-----------|----------|----------|
| GH-01 | Fetch valid GitHub profile | Profile with repos, languages, summary | P0 |
| GH-02 | Fetch non-existent user | Graceful empty result | P1 |
| GH-03 | AnalyzeRepoCodeAsync — finds relevant repos | Code samples returned | P0 |
| GH-04 | AnalyzeRepoCodeAsync — no matching repos | Empty result | P1 |
| GH-05 | Rate limiting handled | Appropriate error or retry | P1 |
| GH-06 | README excerpt truncation | Excerpt within size limit | P2 |

### 1.5 Model Validation

| Test ID | Test Case | Expected | Priority |
|---------|-----------|----------|----------|
| MV-01 | CandidateEvaluation default values | Id generated, scores default to 0 | P2 |
| MV-02 | JobDescription default values | Id generated, CreatedAt set | P2 |
| MV-03 | Resume default values | Id generated, UploadedAt set | P2 |

---

## 2. Integration Tests

### 2.1 Controller Integration Tests

| Test ID | Endpoint | Method | Test Case | Expected | Priority |
|---------|----------|--------|-----------|----------|----------|
| CT-01 | `/api/resume/upload` | POST | Upload valid PDF | 200 + Resume object | P0 |
| CT-02 | `/api/resume/upload` | POST | Upload with no file | 400 | P0 |
| CT-03 | `/api/resume/upload` | POST | Upload unsupported type (.exe) | 400 | P0 |
| CT-04 | `/api/resume/upload` | POST | Upload exceeds 10MB | 413 | P1 |
| CT-05 | `/api/resume` | GET | List all resumes | 200 + array (no ExtractedText) | P0 |
| CT-06 | `/api/resume/{id}` | DELETE | Delete existing resume | 200 + message | P0 |
| CT-07 | `/api/resume/{id}` | DELETE | Delete non-existent resume | 404 | P1 |
| CT-08 | `/api/jobdescription` | POST | Create valid JD | 200 + JD object | P0 |
| CT-09 | `/api/jobdescription` | POST | Create JD missing title | 400 | P0 |
| CT-10 | `/api/jobdescription` | GET | List all JDs | 200 + array | P0 |
| CT-11 | `/api/evaluation/evaluate` | POST | Valid resume + JD | 200 + evaluation | P0 |
| CT-12 | `/api/evaluation/evaluate` | POST | Missing resumeId | 400 | P0 |
| CT-13 | `/api/evaluation/evaluate` | POST | Non-existent resume | 404 | P1 |
| CT-14 | `/api/evaluation/{jdId}` | GET | Get evaluations for JD | 200 + array | P0 |
| CT-15 | `/api/evaluation/{id}` | DELETE | Delete evaluation | 200 | P0 |
| CT-16 | `/api/evaluation/evaluate-all/{jdId}` | POST | Evaluate all resumes | 200 + array | P0 |
| CT-17 | `/api/evaluation/questionnaire/{id}` | POST | Generate for recommended candidate | 200 + questionnaire | P0 |
| CT-18 | `/api/evaluation/questionnaire/{id}` | POST | Generate for non-recommended | 400 | P1 |
| CT-19 | `/api/evaluation/l1-feedback` | POST | Submit valid answers | 200 + feedback | P0 |
| CT-20 | `/api/evaluation/l1-feedback` | POST | Empty answers array | 400 | P1 |
| CT-21 | `/api/evaluation/l2-questionnaire/{id}` | POST | Generate L2 for recommended | 200 + L2 questionnaire | P0 |
| CT-22 | `/api/evaluation/l2-feedback` | POST | Submit L2 answers | 200 + L2 assessment | P0 |
| CT-23 | `/api/evaluation/seed` | POST | Seed from assets | 200 + seed result | P1 |
| CT-24 | `/api/evaluation/evaluate-stream` | POST | SSE stream with progress | SSE events + final result | P0 |

### 2.2 AI Service Integration Tests (Mock-based)

| Test ID | Test Case | Expected | Priority |
|---------|-----------|----------|----------|
| AI-01 | EvaluateResumeAsync returns valid JSON | All fields populated, scores 1–10 | P0 |
| AI-02 | GenerateQuestionnaireAsync returns questions | 10–15 questions with required fields | P0 |
| AI-03 | EvaluateL1AnswersAsync returns feedback | Per-answer evaluations + overall score | P0 |
| AI-04 | AI timeout handling | Graceful error after timeout | P1 |
| AI-05 | Malformed AI response handling | Exception with clear message | P1 |
| AI-06 | Markdown code fence stripping | JSON extracted from ```json blocks | P1 |

---

## 3. End-to-End Tests

### 3.1 Critical User Journeys

| Test ID | Journey | Steps | Priority |
|---------|---------|-------|----------|
| E2E-01 | **Full Evaluation Pipeline** | Upload resume → Create JD → Evaluate → View results | P0 |
| E2E-02 | **Bulk Evaluation** | Seed data → Evaluate All → Compare candidates | P0 |
| E2E-03 | **L1 Interview Flow** | Evaluate → Generate questionnaire → Submit answers → View L1 feedback | P0 |
| E2E-04 | **L2 Interview Flow** | L1 recommended → Generate L2 → Submit answers → Hiring decision | P0 |
| E2E-05 | **Delete Flow** | Upload → Evaluate → Delete resume → Verify cascade | P1 |
| E2E-06 | **Duplicate Prevention** | Evaluate same resume+JD twice → Same result returned | P0 |
| E2E-07 | **Resume Upload via UI** | Select file + LinkedIn + GitHub → Upload → Verify in list | P1 |
| E2E-08 | **SSE Progress Streaming** | Evaluate → Verify progress events → Final result | P1 |

### 3.2 UI Component Tests (React)

| Test ID | Component | Test Case | Priority |
|---------|-----------|-----------|----------|
| UI-01 | Resume Upload | File selected, form submitted, appears in list | P0 |
| UI-02 | JD Selection | Evaluate button disabled until JD selected | P0 |
| UI-03 | Evaluation Display | Scores render correctly with radar chart | P1 |
| UI-04 | Progress Bar | SSE events update progress bar and step messages | P1 |
| UI-05 | Error Handling | API errors displayed in UI | P0 |
| UI-06 | Delete Confirmation | Delete resume triggers confirmation and removal | P1 |
| UI-07 | L1 Questionnaire | Questions rendered, answers submitted | P1 |
| UI-08 | L2 Challenges | All 4 challenge types rendered, answers submitted | P1 |
| UI-09 | Comparison View | Multiple evaluations displayed side-by-side | P1 |

---

## 4. Non-Functional Tests

### 4.1 Performance

| Test ID | Scenario | Target | Priority |
|---------|----------|--------|----------|
| PF-01 | Single evaluation response time | < 60s (AI-dependent) | P0 |
| PF-02 | Evaluate All (10 candidates) | < 10 min | P1 |
| PF-03 | Resume upload + parse (10MB PDF) | < 5s | P1 |
| PF-04 | GET endpoints response time | < 200ms | P0 |
| PF-05 | Concurrent evaluations (5 simultaneous) | No data corruption | P1 |

### 4.2 Security

| Test ID | Scenario | Expected | Priority |
|---------|----------|----------|----------|
| SC-01 | Upload file with path traversal name (`../../etc/passwd`) | Filename sanitized | P0 |
| SC-02 | Upload non-resume file disguised as .pdf | Graceful error | P1 |
| SC-03 | API key not exposed in responses | No secrets in API output | P0 |
| SC-04 | CORS enforcement | Only localhost:3000 allowed | P1 |
| SC-05 | Request size limit enforced | > 10MB rejected | P1 |
| SC-06 | JSON injection in resume text | AI prompt not exploitable | P1 |

### 4.3 Reliability

| Test ID | Scenario | Expected | Priority |
|---------|----------|----------|----------|
| RL-01 | AI service unavailable | Timeout error returned, no crash | P0 |
| RL-02 | GitHub API rate limited | Evaluation completes without GitHub data | P0 |
| RL-03 | Corrupted JSON data file | Graceful error, app doesn't crash | P1 |
| RL-04 | Disk full during file save | Clear error message | P2 |

---

## 5. Test Infrastructure

### 5.1 Recommended Test Framework

| Layer | Tool | Purpose |
|-------|------|---------|
| Unit Tests | xUnit + Moq | C# unit tests with mocking |
| Integration | `WebApplicationFactory<Program>` | In-memory API testing |
| UI Tests | Jest + React Testing Library | Component rendering + interaction |
| E2E | Playwright | Cross-browser full-stack tests |
| Load | k6 or Artillery | Performance testing |
| API Contract | Spectral + OpenAPI spec | Validate API against spec |

### 5.2 Test Data Strategy

- **Mock AI responses:** Store sample AI JSON responses in test fixtures to avoid real API calls in unit/integration tests
- **Test resumes:** Include sample PDF/DOCX/TXT files in test assets
- **Deterministic IDs:** Seed test data with known IDs for predictable assertions
- **Isolated storage:** Use temp directories for `FileStorageService` in tests

### 5.3 CI/CD Integration

```
PR Build → Unit Tests → Integration Tests → Build Container → E2E Tests → Deploy
              ↓               ↓                                   ↓
          Coverage ≥ 80%   AI mocked            Playwright against staging
```

---

## 6. Known Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| AI response format changes | Deserialization failures | Contract tests + response validation |
| GitHub API rate limits | Incomplete evaluations | Caching + graceful degradation |
| Large resume files | OOM or timeouts | File size limits + streaming extraction |
| Concurrent file storage writes | Data corruption | File locking or switch to DB |
| SSE connection drops | Incomplete progress | Client-side reconnection + fallback API |
| Prompt injection via resume text | AI manipulation | Input sanitization + output validation |

---

## 7. Test Prioritization Summary

| Priority | Count | Description |
|----------|-------|-------------|
| P0 (Critical) | ~35 | Core functionality, must pass before release |
| P1 (Important) | ~30 | Important edge cases, should pass |
| P2 (Nice to have) | ~10 | Minor scenarios, can defer |

**Recommended initial coverage target: 80% on unit + integration tests (P0 + P1)**
