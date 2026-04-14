# Contributing to Resume Rating System

Thank you for your interest in contributing! Here's how to get started.

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/your-username/ResumeRating.git`
3. Follow the [LOCAL_SETUP.md](LOCAL_SETUP.md) guide to set up your environment
4. Create a feature branch: `git checkout -b feature/your-feature-name`

## Development Workflow

1. Make your changes
2. Ensure the backend builds: `cd ResumeRating.Api && dotnet build`
3. Test your changes manually
4. Commit with a clear message (see below)
5. Push to your fork and open a Pull Request

## Code Structure

```
ResumeRating.Api/
├── Controllers/    # API endpoints — thin, delegate to services
├── Models/         # Domain models — plain C# classes
└── Services/       # Business logic
    ├── IAiService               # AI provider interface
    ├── AnthropicAiService       # Anthropic Claude implementation
    ├── IEvaluationService       # Core evaluation orchestration
    ├── IResumeParserService     # Resume text extraction
    └── IStorageService          # Data persistence

resume-rating-ui/
└── src/
    ├── App.tsx     # Main React component (all UI)
    └── api.ts      # API client and TypeScript types
```

## Guidelines

### Backend (.NET)
- Follow existing patterns — services implement interfaces, controllers are thin
- Use `async/await` throughout
- Keep AI prompts in the service methods (co-located with the logic)
- Don't add dependencies without discussion

### Frontend (React)
- Keep it in `App.tsx` unless it grows significantly
- Use inline styles (matching the existing approach)
- All API types go in `api.ts`

### Commit Messages

Use clear, descriptive commit messages:
```
feat: add bulk resume evaluation endpoint
fix: handle empty PDF files gracefully
docs: update setup instructions for Windows
refactor: extract score calculation to helper
```

## What to Contribute

- Bug fixes
- New resume format support (e.g., LinkedIn JSON export)
- UI improvements
- Additional evaluation dimensions
- Test coverage
- Documentation improvements

## Pull Request Process

1. Ensure `dotnet build` passes with no errors
2. Update documentation if you changed behavior
3. Describe what your PR does and why
4. Link any related issues

## Questions?

Open an issue for discussion before starting large changes.
