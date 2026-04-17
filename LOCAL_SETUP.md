# Local Setup Guide

Step-by-step instructions to get the Resume Rating System running on your machine.

## Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| .NET SDK | 9.0+ | [Download](https://dotnet.microsoft.com/download/dotnet/9.0) |
| Node.js | 18+ | [Download](https://nodejs.org) |
| Anthropic API Key | — | [Get key](https://console.anthropic.com/settings/keys) |

Verify installations:

```bash
dotnet --version    # Should show 9.x
node --version      # Should show 18.x+
npm --version       # Should show 9.x+
```

## 1. Clone the Repository

```bash
git clone https://github.com/your-org/ResumeRating.git
cd ResumeRating
```

## 2. Configure the API Key

**Option A — Environment variable (recommended):**

```bash
export Anthropic__ApiKey="sk-ant-your-key-here"
```

**Option B — appsettings.json:**

Edit `ResumeRating.Api/appsettings.json`:

```json
{
  "Anthropic": {
    "ApiKey": "sk-ant-your-key-here",
    "Model": "claude-sonnet-4-20250514"
  }
}
```

**Option C — .NET User Secrets (most secure for local dev):**

```bash
cd ResumeRating.Api
dotnet user-secrets init
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-your-key-here"
```

> **Never commit API keys to source control.** The `.gitignore` excludes `appsettings.Development.json`.

## 3. Run the Backend

```bash
cd ResumeRating.Api
dotnet run
```

You should see:

```
Now listening on: http://localhost:5073
Application started.
```

Verify it's working:

```bash
curl http://localhost:5073/api/resume
# Should return: []
```

## 4. Run the Frontend

In a **new terminal**:

```bash
cd resume-rating-ui
npm install --legacy-peer-deps
npm start
```

The app opens at `http://localhost:3000`.

## 5. Load Sample Data

Either:

- Click **"Load Sample Data"** button in the UI header, or
- Run: `curl -X POST http://localhost:5073/api/evaluation/seed`

This loads the 7 sample resumes from `assets/` and the job description from `assets/job-description.md`.

## Changing the AI Model

Edit `appsettings.json` or set via environment variable:

```bash
export Anthropic__Model="claude-sonnet-4-20250514"
```

Available models: `claude-sonnet-4-20250514`, `claude-opus-4-20250514`, `claude-3-5-haiku-20241022`

## Data Storage

All data is stored as JSON files in `ResumeRating.Api/App_Data/`:

- `resumes.json` — Uploaded resumes with extracted text
- `job_descriptions.json` — Job descriptions
- `evaluations.json` — AI evaluation results
- `questionnaires.json` — Generated interview questions
- `l1_feedback.json` — L1 round feedback

To reset all data, delete the `App_Data/` folder and restart the backend.

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Port 5073 already in use | `lsof -ti:5073 \| xargs kill -9` |
| Port 3000 already in use | `lsof -ti:3000 \| xargs kill -9` |
| `npm start` missing script | Run from the `resume-rating-ui/` directory, not project root |
| CORS errors in browser | Ensure backend is running on port 5073 |
| `Anthropic:ApiKey not configured` | Set the key via env var, appsettings, or user-secrets |
| PDF parsing fails | Ensure the PDF is text-based, not scanned images |
