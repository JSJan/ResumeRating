# Prompting Feedback & Improvement Notes

## How This Project Was Built

This entire application was built through **7 natural language prompts** in a single AI-assisted coding session using GitHub Copilot (Claude). No manual coding was required — every feature, from backend to frontend, was generated, debugged, and iterated through conversational prompts.

---

## Prompt Journey & Lessons Learned

### Prompt 1 — Core Vision
> "I want to build a project where it reads data from provided resumes, rates the profiles based on the job description..."

**What worked:** Clear, descriptive requirements. Listing the 7 rating dimensions gave the AI a concrete structure to work with.

**What could improve:** Specifying the tech stack upfront (e.g., ".NET + React") would have saved a clarification step.

**Lesson:** Be specific about *what* you want evaluated, not *how* to build it. Let the AI pick the architecture.

---

### Prompt 2 — Adapting to Reality
> "File upload failed, use the pdf available in assets folder instead"

**What worked:** Pivoting quickly when a feature didn't work. The seed endpoint approach turned out to be better for demos anyway.

**What could improve:** Could have asked for both — keep upload AND add seed as a fallback.

**Lesson:** Don't fight broken features. Ask for alternatives that solve the same problem differently.

---

### Prompt 3 — Documentation & Provider Switch
> "Add readme, changelog, localsetup, contributing, architecture. Use anthropic API."

**What worked:** Batching documentation requests together. Switching AI providers was seamless due to the interface-based design.

**What could improve:** Should have verified API key access before requesting the switch. Led to Anthropic → GitHub Models pivot.

**Lesson:** Infra changes (API keys, providers) should be validated before building features on them.

---

### Prompt 4 — Expanding Scope
> "Want a overall comparison of all profiles uploaded, not just single candidate evaluation"

**What worked:** Concise request, clear gap identification. The comparison table with sortable columns was exactly right.

**What could improve:** Could have specified the comparison criteria (scores only? or include qualitative summaries?).

**Lesson:** Feature requests work best when you describe the *outcome* you want, not the implementation.

---

### Prompt 5 — Debugging & Feature Addition
> "Why am I not able to do comparison before evaluating? Add L2 round for hands on, system design..."

**What worked:** Combining a bug report with a feature request. The "Evaluate All" button solved the UX gap elegantly.

**What could improve:** The L2 round categories (system design, coding, design thinking, trade-offs) were well chosen.

**Lesson:** When something doesn't work as expected, describe the *user experience* gap, not just the technical error.

---

### Prompt 6 — Meta & Enhancement Planning
> "How to enhance the functionality? Create a new md for feedback..."

**What worked:** Stepping back to think about the product holistically. The ENHANCEMENTS.md captures a full roadmap.

**What could improve:** Could have prioritized 2-3 enhancements for immediate implementation.

**Lesson:** Periodically ask for a "product review" prompt to get a fresh perspective on gaps.

---

### Prompt 7 — Outreach & IP Research
> "Create a LinkedIn post, check for similar projects, can I file a patent?"

**What worked:** Thinking about the project beyond code — communication, market positioning, IP protection.

**Lesson:** An AI coding assistant can also help with non-code work like marketing, research, and strategy.

---

## What Makes Good AI Prompts (For Coding)

| Do | Don't |
|----|-------|
| Describe the *outcome* you want | Dictate exact code structure |
| List specific dimensions/fields | Say "make it good" |
| Mention constraints (tech stack, time) | Assume the AI knows your env |
| Combine related requests | Send 10 tiny prompts for one feature |
| Ask "why doesn't X work?" with context | Just say "it's broken" |
| Request alternatives when blocked | Keep retrying the same approach |

---

## Metrics

| Metric | Value |
|--------|-------|
| Total prompts | 7 |
| Backend endpoints | 12 |
| Frontend tabs | 8 |
| AI models/services | 3 (OpenAI → Anthropic → GitHub Models) |
| Lines of C# (approx) | ~900 |
| Lines of TypeScript (approx) | ~750 |
| Documentation files | 7 (README, CHANGELOG, LOCAL_SETUP, CONTRIBUTING, ARCHITECTURE, ENHANCEMENTS, this file) |
| Manual code written | 0 lines |
| Provider switches | 3 |
| Build errors fixed | 5+ |

---

## Areas for Improvement in the Prompting Process

1. **State management across prompts** — Each prompt builds on previous context. Long sessions risk losing context. Summarize state periodically.
2. **Testing was skipped** — No unit tests, no integration tests. Should have asked for tests alongside features.
3. **Error handling is minimal** — Most errors show generic messages. A prompt like "add proper error handling with user-friendly messages" would help.
4. **No CI/CD** — Could have asked for GitHub Actions workflow in a single prompt.
5. **Security review** — Should have prompted for a security audit before considering production use.
6. **Accessibility** — No ARIA labels, no keyboard navigation. Should be part of the UI prompt.
