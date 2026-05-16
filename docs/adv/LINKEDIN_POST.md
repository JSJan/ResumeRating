# I Built a Full AI Hiring Pipeline in 7 Prompts — Here's What I Learned

I challenged myself to build a complete AI-powered resume screening and interview evaluation system using **only natural language prompts** with GitHub Copilot. No manual coding. Just conversation.

## What it does

**Resume Rating System** — an end-to-end hiring pipeline that:

- Parses PDF/DOCX resumes and rates candidates across **7 dimensions** (experience, work history, education, side projects, job fit, aww factor, uniqueness)
- **Compares all candidates** side-by-side with sortable rankings
- Auto-generates **L1 interview questionnaires** tailored to each candidate
- Evaluates L1 answers and recommends candidates for the next round
- Runs an **L2 tech round** covering system design, hands-on coding, design thinking, and trade-off analysis
- Provides a **final hiring recommendation** (Strong Hire / Hire / Lean No Hire / No Hire)

## The stack

- .NET 9 Web API (backend)
- React + TypeScript (frontend)
- GPT-4o via GitHub Models (AI engine)
- File-based JSON storage (zero-setup)

## The 7 prompts

1. **"Rate resumes against a job description"** → Full backend + frontend scaffold
2. **"Use assets folder PDFs instead"** → Seed data feature
3. **"Add docs + switch to Anthropic"** → 5 markdown files + provider swap
4. **"Compare all profiles, not just one"** → Comparison table with sortable scores
5. **"Add L2 round for system design & coding"** → 4-challenge tech assessment
6. **"What can be improved?"** → 10-area enhancement roadmap
7. **"Similar projects? Can I patent this?"** → Market research + IP analysis

## What surprised me

- **Provider switching was painless.** The AI generated an interface-based design from the start. Switching from OpenAI → Anthropic → GitHub Models was just a DI registration change.
- **Debugging worked through conversation.** Build errors, npm conflicts, auth failures — all resolved by describing the error and letting the AI fix it.
- **7 prompts produced 12 API endpoints, 8 UI tabs, and ~1,700 lines of working code.**

## Key takeaways for AI-assisted development

1. **Describe outcomes, not implementations.** "Compare all candidates" works better than "add a table with columns for each score."
2. **Batch related requests.** One prompt for 5 docs is better than 5 separate prompts.
3. **Pivot fast.** When file upload broke, I asked for an alternative instead of debugging the upload.
4. **Think in rounds.** Each prompt built on the previous one. The AI maintained context across the session.

## Is this the future of hiring tech?

There are ~130 open-source resume screening projects on GitHub, mostly Python/Streamlit tools that do basic ML classification or keyword matching. None combine:

- Multi-dimensional AI scoring
- Auto-generated interview questions
- Multi-round evaluation (L1 screening + L2 tech assessment)
- Candidate comparison and ranking
- Hiring recommendations

The space is wide open for a **full-pipeline** solution that covers screening through final decision.

## What's next

- HR round (culture fit, salary negotiation)
- Leadership round (for senior roles)
- Pipeline dashboard with auto-advancement
- Database migration for production use

---

Tech: #DotNet #React #TypeScript #AI #GPT4 #GitHubModels #OpenAI #Hiring #RecruitmentTech #AIAssisted #CopilotChat #VibeCoding

Built with GitHub Copilot | Code on GitHub
