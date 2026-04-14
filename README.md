# Resume Rating System

AI-powered resume evaluation and interview preparation tool built with **.NET 9 Web API**, **React + TypeScript**, and **Anthropic Claude**.

## Features

- **Resume Parsing** — Parse PDF, DOCX, and TXT resumes with iText7 and OpenXml
- **AI Evaluation** — Rate candidates on 7 dimensions using Claude (experience, work history, education, side projects, job fit, aww factor, uniqueness)
- **Salary Estimation** — Estimate current package, role, and expected salary
- **L1 Questionnaire** — Auto-generate personalized interview questions for recommended candidates
- **L1 Feedback** — Evaluate interview answers and get tech hands-on round recommendations
- **Seed Data** — One-click loading of sample resumes from `assets/` folder

## Quick Start

```bash
# 1. Set your Anthropic API key
cd ResumeRating.Api
export Anthropic__ApiKey="sk-ant-your-key-here"

# 2. Start backend
dotnet run

# 3. Start frontend (new terminal)
cd resume-rating-ui
npm install --legacy-peer-deps
npm start
```

- Backend: `http://localhost:5073`
- Frontend: `http://localhost:3000`

Click **"Load Sample Data"** in the UI to load all resumes from `assets/` and the job description.

See [LOCAL_SETUP.md](LOCAL_SETUP.md) for detailed setup instructions.

## Documentation

- [LOCAL_SETUP.md](LOCAL_SETUP.md) — Detailed local development setup
- [ARCHITECTURE.md](ARCHITECTURE.md) — System architecture and design decisions
- [CONTRIBUTING.md](CONTRIBUTING.md) — How to contribute
- [CHANGELOG.md](CHANGELOG.md) — Version history

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/resume/upload` | Upload a resume (PDF/DOCX/TXT) |
| GET | `/api/resume` | List all uploaded resumes |
| POST | `/api/jobdescription` | Create a job description |
| GET | `/api/jobdescription` | List all job descriptions |
| POST | `/api/evaluation/evaluate` | Evaluate a resume against a JD |
| GET | `/api/evaluation/{jdId}` | Get evaluations for a JD |
| POST | `/api/evaluation/questionnaire/{evalId}` | Generate L1 questionnaire |
| POST | `/api/evaluation/l1-feedback` | Submit answers and get L1 feedback |
| POST | `/api/evaluation/seed` | Load sample resumes + JD from assets |

## Workflow

1. **Upload** resumes (or click **Load Sample Data**)
2. **Create** a job description with required/preferred skills
3. **Evaluate** each resume against the JD — get scores, feedback, and salary estimates
4. **Generate questionnaire** for candidates recommended for L1
5. **Submit answers** from the L1 interview to get detailed feedback and tech round recommendations

## License

MIT
