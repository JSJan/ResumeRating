# Token Usage, Prompts & Cost Optimization

## Overview

This document analyzes the AI token usage across all API calls, the exact prompts used, and strategies for cost optimization.

---

## AI Provider Configuration

| Provider | Model | Used For | Max Tokens (Response) | Timeout |
|----------|-------|----------|----------------------|--------|
| GitHub Models (default) | `gpt-4o` | Resume evaluation | Not explicitly limited | 120s |
| GitHub Models (light) | `gpt-4o-mini` | L1/L2 questionnaire & answer evaluation | Not explicitly limited | 120s |
| Anthropic (alternative) | `claude-4-sonnet` | Resume evaluation (disabled) | 4,096 | Default |

> **Implemented:** Multi-model support is active. `GitHub:LightModel` defaults to `gpt-4o-mini` and is used for all non-evaluation AI calls.

---

## Token Usage Per API Call

### 1. Resume Evaluation (`POST /api/evaluation/evaluate`)

**Prompt size (estimated, post-optimization):**

| Component | Before Optimization | After Optimization |
|-----------|---------------------|---------------------|
| System prompt | ~30 tokens | ~30 tokens |
| Evaluation instructions | ~1,800 tokens | ~600 tokens |
| Job description content | ~200–500 tokens | ~200–500 tokens |
| Resume text (PDF extracted) | ~500–3,000 tokens | ~500–2,000 tokens (truncated at 8k chars) |
| LinkedIn section | ~50–100 tokens (always present) | ~0–100 tokens (conditional) |
| GitHub profile summary | ~200–500 tokens (always present) | ~0–500 tokens (conditional) |
| GitHub code analysis | ~1,000–5,000 tokens | ~1,000–5,000 tokens |
| Pre-computed analysis hints | N/A | ~150–300 tokens |
| JSON format instructions | ~800 tokens | ~200 tokens (compact schema) |
| **Total Input** | **~3,500–11,000 tokens** | **~2,500–9,000 tokens** |
| **Expected Output** | **~1,500–3,000 tokens** | **~1,500–3,000 tokens** |

**Cost per call (GPT-4o pricing, post-optimization):**

- Input: $2.50/1M tokens → $0.006–$0.023 (was $0.009–$0.028)
- Output: $10.00/1M tokens → $0.015–$0.030
- **Total: ~$0.021–$0.053 per evaluation** (was ~$0.024–$0.058)

**Optimizations applied:** ✅ Prompt consolidation, ✅ resume truncation, ✅ conditional sections, ✅ compact JSON schema, ✅ pre-computed hints

---

### 2. L1 Questionnaire Generation (`POST /api/evaluation/questionnaire/{evaluationId}`) — ✅ Uses GPT-4o-mini

| Component | Estimated Tokens |
|-----------|-----------------|
| System prompt | ~30 tokens |
| Candidate profile summary | ~150 tokens |
| Job description | ~200–500 tokens |
| Resume text | ~500–3,000 tokens |
| Instructions + JSON format | ~300 tokens |
| **Total Input** | **~1,200–4,000 tokens** |
| **Expected Output** | **~800–1,500 tokens** |

**Cost per call (GPT-4o-mini pricing): ~$0.0007–$0.002** (was ~$0.011–$0.025 with GPT-4o)

---

### 3. L1 Answer Evaluation (`POST /api/evaluation/l1-feedback`) — ✅ Uses GPT-4o-mini

| Component | Estimated Tokens |
|-----------|-----------------|
| System prompt | ~30 tokens |
| Candidate info | ~50 tokens |
| Job description | ~100 tokens |
| Q&A pairs (10–15 questions) | ~1,000–2,500 tokens |
| Instructions + JSON format | ~300 tokens |
| **Total Input** | **~1,500–3,000 tokens** |
| **Expected Output** | **~1,000–2,000 tokens** |

**Cost per call (GPT-4o-mini pricing): ~$0.0008–$0.001** (was ~$0.014–$0.025 with GPT-4o)

---

### 4. L2 Questionnaire Generation (`POST /api/evaluation/l2-questionnaire/{evaluationId}`) — ✅ Uses GPT-4o-mini

| Component | Estimated Tokens |
|-----------|-----------------|
| System prompt | ~30 tokens |
| Candidate profile + L1 feedback | ~300 tokens |
| Job description | ~200–500 tokens |
| Resume text | ~500–3,000 tokens |
| Instructions + JSON format | ~400 tokens |
| **Total Input** | **~1,400–4,200 tokens** |
| **Expected Output** | **~800–1,500 tokens** |

**Cost per call (GPT-4o-mini pricing): ~$0.0007–$0.002** (was ~$0.012–$0.025 with GPT-4o)

---

### 5. L2 Answer Evaluation (`POST /api/evaluation/l2-feedback`) — ✅ Uses GPT-4o-mini

| Component | Estimated Tokens |
|-----------|-----------------|
| System prompt | ~30 tokens |
| Candidate info | ~50 tokens |
| 4 challenges + candidate answers | ~1,500–4,000 tokens |
| Instructions + JSON format | ~300 tokens |
| **Total Input** | **~1,900–4,400 tokens** |
| **Expected Output** | **~800–1,500 tokens** |

**Cost per call (GPT-4o-mini pricing): ~$0.0008–$0.002** (was ~$0.013–$0.025 with GPT-4o)

---

## Full Pipeline Cost Per Candidate

| Stage | Before Optimization | After Optimization | Savings |
|-------|---------------------|--------------------|---------|
| Resume Evaluation (GPT-4o) | $0.024–$0.058 | $0.021–$0.053 | ~10% |
| L1 Questionnaire (GPT-4o-mini) | $0.011–$0.025 | $0.0007–$0.002 | **~93%** |
| L1 Answer Evaluation (GPT-4o-mini) | $0.014–$0.025 | $0.0008–$0.001 | **~94%** |
| L2 Questionnaire (GPT-4o-mini) | $0.012–$0.025 | $0.0007–$0.002 | **~93%** |
| L2 Answer Evaluation (GPT-4o-mini) | $0.013–$0.025 | $0.0008–$0.002 | **~93%** |
| **Total per candidate** | **$0.074–$0.158** | **$0.024–$0.060** | **~60–68%** |

**Evaluate All (10 candidates, eval only):** ~$0.21–$0.53 (was ~$0.24–$0.58)

---

## Exact Prompts Used

### Prompt 1: Resume Evaluation (GPT-4o)

- **Role:** Expert technical recruiter, hiring manager, and senior code reviewer
- **Sections:** Job Description → Resume (truncated to ~8k chars) → LinkedIn (conditional) → GitHub Profile (conditional) → Source Code (conditional) → Pre-Computed Analysis Hints → Consolidated Resume Quality Assessment → Scoring Rules → Compact JSON schema
- **Optimizations applied:** ✅ 3 overlapping sections consolidated into 1, ✅ empty sections removed when no data, ✅ compact single-line JSON template, ✅ pre-computed metrics (keyword match %, readability, AI probability, power verbs, GitHub skill overlap) injected as structured hints
- **Output:** 14 scores (1–10), 14 feedback fields, red flags, buzzword lists, salary estimates, L1 recommendation

### Prompt 2: L1 Questionnaire (GPT-4o-mini)

- **Role:** Expert technical interviewer
- **Input:** Candidate profile, JD, full resume
- **Output:** 10–15 personalized questions with category, difficulty, expected insight
- **Model change:** ✅ Switched from GPT-4o to GPT-4o-mini (~93% cheaper)

### Prompt 3: L1 Answer Evaluation (GPT-4o-mini)

- **Role:** Expert technical interviewer evaluating L1 round answers
- **Input:** Candidate info, JD, all Q&A pairs with expected insights
- **Output:** Per-answer scores + feedback, overall L1 score, tech round recommendation
- **Model change:** ✅ Switched from GPT-4o to GPT-4o-mini (~94% cheaper)

### Prompt 4: L2 Questionnaire (GPT-4o-mini)

- **Role:** Senior technical architect
- **Input:** Candidate profile, L1 feedback, areas to probe, resume, JD
- **Output:** 4 challenges (System Design, Hands-On Coding, Design Thinking, Trade-Off Analysis)
- **Model change:** ✅ Switched from GPT-4o to GPT-4o-mini (~93% cheaper)

### Prompt 5: L2 Answer Evaluation (GPT-4o-mini)

- **Role:** Senior technical architect evaluating L2 answers
- **Input:** 4 challenge scenarios + candidate answers
- **Output:** Per-challenge scores, overall L2 score, hiring recommendation
- **Model change:** ✅ Switched from GPT-4o to GPT-4o-mini (~93% cheaper)

---

## Cost Optimization Strategies

### 1. Replace AI with Internal Logic — ✅ Implemented (`ResumeAnalysisService`)

The following operations are now handled by the local `ResumeAnalysisService` without AI:

| Area | Status | Implementation |
|------|--------|---------------|
| **Candidate name extraction** | ✅ Already internal | Heuristic (first line of text) |
| **Resume text extraction** | ✅ Already internal | iText/OpenXML |
| **Buzzword detection** | ✅ Implemented | Keyword frequency counter — counts JD skill occurrences in resume, flags keywords with 3+ repetitions |
| **AI-generated content detection** | ✅ Implemented | Composite heuristic: sentence length variance, power verb frequency (20 verbs tracked), generic phrase detection, Flesch-Kincaid readability, unique word ratio |
| **Resume-JD keyword matching** | ✅ Implemented | Skill match % computed locally, matched/missing skills lists |
| **Duplicate evaluation check** | ✅ Implemented | Same resume+JD returns cached result instantly |
| **GitHub language stats** | ✅ Implemented | `ComputeGitHubSkillMatch` computes language/topic overlap % locally from GitHub API data |
| **Years of experience extraction** | ✅ Implemented | Regex: "10+ years", date range spans (2015–2020) |
| **Education level detection** | ✅ Implemented | Text matching: PhD, Masters, Bachelors, Associate/Diploma |
| **Basic scoring hints** | ✅ Implemented | Pre-computed metrics passed as structured hints to AI prompt |

**Actual savings per evaluation: ~1,500–2,000 tokens input reduction**

### 2. Prompt Optimization — ✅ Implemented

| Technique | Status | Impact |
|-----------|--------|--------|
| **Remove redundant instructions** | ✅ Done | 3 overlapping sections (Authenticity + Buzzword + AI Detection) consolidated into single "Resume Quality Assessment" — Saved ~500 tokens |
| **Shorten JSON format** | ✅ Done | Single-line compact JSON template instead of verbose field-by-field descriptions — Saved ~600 tokens |
| **Truncate resume text** | ✅ Done | Resume text capped at 8,000 characters (~2,000 tokens) — Saved ~500–1,000 tokens |
| **Conditional sections** | ✅ Done | LinkedIn/GitHub/Code sections only included when data exists; no empty headers — Saved ~100–300 tokens |
| **Use structured output mode** | ❌ Not yet | GPT-4o supports JSON mode — would remove need for "Respond ONLY with JSON" instruction — ~50 tokens |

### 3. Caching Strategies — ✅ Mostly Implemented

| Strategy | Status | Implementation |
|----------|--------|---------------|
| **Cache GitHub profile data** | ✅ Implemented | 24-hour in-memory cache (`ConcurrentDictionary`) in `GitHubProfileService` |
| **Cache code analysis per user** | ✅ Implemented | 24-hour cache keyed by username + sorted skills list |
| **Skip re-evaluation** | ✅ Implemented | `EvaluateAsync` returns cached result if resume+JD already evaluated |
| **Batch evaluations** | ❌ Not yet | Send multiple resumes in a single prompt for comparison mode |

### 4. Model Selection — ✅ Implemented

| Model | Input Cost | Output Cost | Quality | Used For | Status |
|-------|-----------|-------------|---------|----------|--------|
| GPT-4o | $2.50/1M | $10.00/1M | Excellent | Resume evaluation | ✅ Active |
| GPT-4o-mini | $0.15/1M | $0.60/1M | Good | L1/L2 questionnaire + answer evaluation | ✅ Active |
| Claude 3.5 Haiku | $0.25/1M | $1.25/1M | Good | L1/L2 alternative light model | ✅ Wired (set `Anthropic:LightModel`) |

**Configured via:** `GitHub:Model` (default: `gpt-4o`) and `GitHub:LightModel` (default: `gpt-4o-mini`) in appsettings.json

**Actual savings: ~93% on L1/L2 calls**

### 5. Internal Pre-Processing Pipeline — ✅ Implemented

```
Resume Upload → Text Extraction → Local Analysis → AI Evaluation (with hints)
                                        │
                                        ├── Keyword frequency analysis     ✅
                                        ├── Years of experience extraction  ✅
                                        ├── Education level detection       ✅
                                        ├── Skill-JD match percentage      ✅
                                        ├── Buzzword count (3+ repeats)    ✅
                                        ├── Sentence structure variance    ✅
                                        ├── GitHub skill overlap score     ✅
                                        ├── Readability score (Flesch-K.)  ✅
                                        ├── Power verb detection (20 verbs)✅
                                        └── AI-generated probability       ✅
```

Pre-computed metrics passed as structured `## Pre-Computed Analysis` section in AI prompt.

---

## Monthly Cost Projections

| Scenario | Candidates/Month | Full Pipeline | Eval Only | Before Optimization | After Optimization |
|----------|-----------------|---------------|-----------|---------------------|--------------------|
| Small team | 50 | 20 | 50 | $6–$12 | $2–$5 |
| Medium team | 200 | 80 | 200 | $25–$50 | $9–$18 |
| Large org | 1,000 | 400 | 1,000 | $120–$250 | $40–$80 |

---

## Summary — What's Implemented vs Remaining

### ✅ Implemented

1. **Skip duplicate evaluations** — `EvaluateAsync` returns cached result for same resume+JD
2. **Cache GitHub data** — 24h in-memory cache for profiles and code analysis
3. **Truncate resume text** — Capped at 8,000 characters (~2,000 tokens)
4. **Pre-compute buzzword/keyword/readability scores locally** — `ResumeAnalysisService` with 10 metrics
5. **GPT-4o-mini for non-evaluation calls** — L1/L2 questionnaire + answer evaluation (~93% cheaper)
6. **Pre-processing pipeline** — Local analysis results passed as structured hints to AI
7. **Prompt consolidation** — 3 redundant sections merged into 1, compact JSON schema
8. **Conditional prompt sections** — Empty LinkedIn/GitHub sections removed
9. **Runtime token tracking** — `GET /api/evaluation/token-usage` returns per-call token counts, cost estimates, and summary by operation
10. **Claude Haiku integration** — Configurable via `Anthropic:ApiKey` + `Anthropic:LightModel` in appsettings; routes L1/L2 calls through Anthropic when set

### ❌ Remaining

1. **Structured output mode** — Use GPT-4o JSON mode to eliminate "Respond ONLY with JSON" overhead
2. **Batch evaluations** — Send multiple resumes in single prompt for comparison mode
3. **Fine-tuned smaller model** — Train on evaluation outputs for cheaper inference
