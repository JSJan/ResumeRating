# Resume Rating System — Enhancement Roadmap

> Current state: 8 UI tabs, 12 API endpoints, 7 models, file-based storage, GPT-4o via GitHub Models

---

## What Works Today

| Stage | Feature | Status |
|-------|---------|--------|
| Upload | PDF/DOCX/TXT parsing, seed from assets | ✅ Done |
| Screening | 7-dimension AI scoring, salary estimates | ✅ Done |
| Comparison | Sortable ranking, Evaluate All batch | ✅ Done |
| L1 Round | Auto-generated questionnaire, answer evaluation | ✅ Done |
| L2 Round | System design, coding, design thinking, trade-offs | ✅ Done |

---

## Proposed Enhancements

### 1. HR Round (Priority: High)

**Why:** L1/L2 only cover technical fitness. HR round evaluates culture fit, communication, salary negotiation readiness, and red flags.

**Scenarios to cover:**
- Salary expectation vs market rate alignment
- Notice period and availability
- Career motivation and growth expectations
- Team dynamics and conflict resolution
- Company culture alignment
- Work-life balance expectations

**Implementation:**
- New model: `HRAssessment` with sections for compensation, culture, communication, stability
- AI generates HR-specific questions based on resume gaps, job-hopping patterns, career trajectory
- Scoring: Communication (1-10), Culture Fit (1-10), Stability Risk (1-10), Negotiation Readiness (1-10)
- Output: Offer recommendation with suggested CTC band

---

### 2. Leadership Round (Priority: High)

**Why:** For senior roles (5+ years), leadership qualities matter. Current evaluation doesn't assess strategic thinking or people management.

**Scenarios to cover:**
- Handling underperformers in a team
- Driving a project with ambiguous requirements
- Cross-team collaboration and influence without authority
- Technical debt vs feature delivery prioritization
- Mentoring and knowledge sharing
- Stakeholder management and expectation setting

**Implementation:**
- New model: `LeadershipAssessment`
- Sections: Strategic Thinking, People Management, Decision Making, Conflict Resolution, Vision & Influence
- Only triggered for candidates with 5+ years experience or senior-level JDs
- AI evaluates leadership maturity level: Individual Contributor / Emerging Leader / Established Leader / Executive

---

### 3. Automated Pipeline (Priority: Medium)

**Why:** Currently each stage is manual — user must click through each round sequentially. For bulk hiring, this is slow.

**Enhancements:**
- **Auto-advance pipeline:** Upload → Evaluate → auto-shortlist (score ≥ 7) → auto-generate L1 for all shortlisted
- **Pipeline dashboard:** Show each candidate's stage (Screened → L1 → L2 → HR → Offer) with status indicators
- **Batch operations:** Evaluate All already exists; extend to "Generate All L1 Questionnaires" for shortlisted candidates
- **Email/notification hooks:** Placeholder for notifying candidates or interviewers

---

### 4. Smarter Comparison (Priority: Medium)

**Why:** Current comparison is score-based only. Hiring managers need qualitative insights.

**Enhancements:**
- **AI-generated comparison summary:** "Candidate A excels in system design but lacks in..." — a narrative comparing top 3-5 candidates
- **Radar/spider chart data:** Return score arrays for visualization (7 dimensions per candidate)
- **Stack ranking with justification:** AI explains why #1 is ranked above #2
- **Fit matrix:** Map candidates to multiple open roles, not just one JD
- **Export to PDF/CSV:** Download comparison report for offline review

---

### 5. Resume Quality Feedback (Priority: Medium)

**Why:** Candidates often have poorly structured resumes. The system already parses them — it can also coach.

**Enhancements:**
- **Resume improvement suggestions:** Missing sections, weak action verbs, quantification gaps
- **ATS compatibility score:** How well the resume would pass automated screening
- **Keyword match analysis:** Which JD keywords are present/missing in the resume
- **Before/after preview:** Suggested rewrites for weak bullet points

---

### 6. Multi-JD Matching (Priority: Medium)

**Why:** A candidate might not fit Role A but could be perfect for Role B. Currently each evaluation is 1:1.

**Enhancements:**
- **Cross-JD evaluation:** Evaluate one resume against all available JDs
- **Best-fit recommendation:** "This candidate is 9/10 for Backend Engineer but only 5/10 for Full Stack"
- **Talent pool view:** See all candidates across all JDs with their best-fit roles

---

### 7. Data Persistence & History (Priority: High)

**Why:** File-based JSON storage doesn't support concurrent users, has no query capability, and data can be lost.

**Enhancements:**
- **SQLite for local dev:** Zero-config database that supports queries
- **PostgreSQL for production:** Scalable, concurrent-safe
- **Evaluation history:** Track re-evaluations over time, see score progression
- **Audit log:** Who evaluated whom, when, what scores changed

---

### 8. UI/UX Improvements (Priority: High)

**Current pain points:**
- Single-file App.tsx is getting large (700+ lines) — maintainability concern
- No loading skeleton or progress indicators for long AI calls
- No dark mode
- Tabs don't show completion status (which stages are done for a candidate)
- No way to go back to a previous evaluation without re-evaluating

**Enhancements:**
- **Component-based architecture:** Split into ResumeUpload, Evaluation, Comparison, L1Round, L2Round components
- **Progress tracker:** Visual pipeline showing candidate's journey through stages
- **Candidate detail page:** Click a candidate name → see all their evaluations, scores, and round history in one view
- **Real-time AI progress:** Stream AI responses instead of waiting for full completion
- **Mobile-responsive layout:** Current inline styles don't handle smaller screens
- **Dashboard landing page:** Summary stats — total resumes, evaluations this week, top candidates, pending L1s

---

### 9. Security & Production Readiness (Priority: High)

**Current gaps:**
- No authentication — anyone with the URL can access
- API keys in user-secrets (good for dev, not for prod)
- No rate limiting on AI calls
- No input validation on resume file size/type beyond frontend hints

**Enhancements:**
- **Authentication:** Add OAuth2/OpenID Connect (Azure AD, Google, etc.)
- **Role-based access:** Recruiter (upload, evaluate), Interviewer (L1/L2 only), Hiring Manager (comparison, decisions)
- **API rate limiting:** Prevent abuse of AI endpoints
- **Resume sanitization:** Validate file content, not just extension
- **Secrets management:** Azure Key Vault or AWS Secrets Manager for production
- **HTTPS enforcement:** Currently HTTP-only in dev

---

### 10. AI Provider Flexibility (Priority: Low)

**Current state:** GitHub Models (GPT-4o) is the only active provider. Anthropic is implemented but disabled.

**Enhancements:**
- **Provider selector in UI:** Let user choose GPT-4o, Claude, Gemini, or local models per evaluation
- **Cost tracking:** Log token usage per evaluation for budget monitoring
- **Prompt versioning:** Store prompt templates separately, track which version produced which result
- **Evaluation consistency:** Run same resume through multiple models, compare scores for reliability

---

## Need of the Hour — Top 5 Priorities

| # | Enhancement | Impact | Effort |
|---|-------------|--------|--------|
| 1 | **HR Round** | Completes the hiring pipeline end-to-end | Medium |
| 2 | **Leadership Round** | Critical for senior hires, biggest gap today | Medium |
| 3 | **Pipeline Dashboard** | Makes the app usable for real hiring workflows | Medium |
| 4 | **Candidate Detail View** | Eliminates re-evaluation, shows full history | Low |
| 5 | **Database Migration** | Unblocks multi-user, concurrent access | Medium |

---

## Feedback for Improvement

### What's Working Well
- AI evaluation quality is good — 7 dimensions cover technical fitness comprehensively
- Seed data makes demo/testing fast
- Single-page React app is quick to iterate on
- IAiService abstraction makes provider swaps easy

### What Needs Work
- **No candidate journey view** — you lose context switching between tabs; need a unified candidate profile page
- **Manual round advancement** — each round requires explicit user action; should auto-suggest next steps
- **No persistence of tab state** — refreshing the page loses all in-memory evaluations
- **Comparison requires prior evaluation** — the "Evaluate All" button solves this, but UX could auto-prompt
- **Long AI response times** — consider streaming responses or showing intermediate progress
- **No way to edit/re-evaluate** — if a score seems wrong, you must re-run the full evaluation

### Usability Gaps
- Tab bar overflows on smaller screens (now 8 tabs)
- No tooltips or help text explaining what each dimension means
- No confirmation before triggering expensive AI operations
- Error messages are generic — should guide user on what to do next
