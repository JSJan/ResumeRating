# Market Research & Patent Analysis

## Similar Projects on the Internet

### Open Source (GitHub — ~130+ repositories in "resume-screening" topic)

| Project | Tech | What It Does | Gap vs Ours |
|---------|------|-------------|-------------|
| **Resume-Screening-RAG-Pipeline** | Python, LangChain, GPT-4 | RAG-based chatbot for resume screening | No scoring, no interview rounds |
| **AI-powered-Resume-Screener-Bot** | Flask, React, IBM Watsonx | Resume analysis + bias detection | No multi-round evaluation |
| **ResuMate** | Python, Streamlit, OpenAI, LlamaIndex | Resume rating via LLM | Single score, no comparison, no interview |
| **ML-Driven-Talent-Screening-System** | Python, Streamlit, NLP/ML | NLP-based resume classification | ML classification only, no LLM evaluation |
| **Resume-Screening (viditkhemka)** | Angular, Node.js, NLP | Resume scanning and analysis | No AI scoring, basic keyword match |
| **cv-compass-client** | TypeScript, Llama3, HuggingFace | Resume optimization for ATS | Candidate-facing, not recruiter tool |
| **frappe_ai_hiring** | Python, Frappe ERPNext | End-to-end hiring in ERPNext | Tied to Frappe ecosystem |
| **AI-Resume-Rating-Model** | Python, Groq Llama-3 | Resume scoring against JD | Single-dimension, no interview pipeline |
| **Interview-Now** | JavaScript | AI interview platform (HR/Manager/Tech) | Has interview types but no resume scoring |
| **HireAI** | JavaScript | Match percentage + skill gap | No multi-round evaluation |

### Commercial Products

| Product | What It Does | Pricing | Gap vs Ours |
|---------|-------------|---------|-------------|
| **HireVue** | AI video interviews + assessments | Enterprise ($$$) | Video-based, not resume-first |
| **Pymetrics/Harver** | Behavioral assessments + AI matching | Enterprise | Gamified assessments, not resume analysis |
| **Greenhouse** | ATS + structured hiring | $6k+/year | ATS workflow, minimal AI scoring |
| **Lever** | ATS + candidate relationship | Enterprise | Recruitment CRM, not evaluation |
| **Ideal (now Ceridian)** | AI resume screening | Enterprise | Screening only, no interview generation |
| **HiPeople** | AI reference checks + assessments | $300+/month | Post-interview, not pre-screening |
| **Manatal** | AI recruitment + resume parsing | $15/user/month | Basic AI matching, no multi-round |
| **Paradox (Olivia)** | AI chatbot for recruiting | Enterprise | Conversational, not evaluation |

---

## What Makes This Project Unique

| Feature | Ours | Typical Open Source | Commercial ATS |
|---------|------|-------------------|----------------|
| Multi-dimensional scoring (7 categories) | Yes | No (single score) | Partial |
| Auto-generated interview questions | Yes | No | No |
| L1 answer evaluation with AI | Yes | No | No |
| L2 tech assessment (4 challenge types) | Yes | No | No |
| Side-by-side candidate comparison | Yes | No | Yes (basic) |
| Salary estimation | Yes | No | No |
| Hiring recommendation (Strong Hire → No Hire) | Yes | No | No |
| Full pipeline (screen → L1 → L2 → decision) | Yes | No | Partial |
| Provider-agnostic AI (swap LLM easily) | Yes | No | No |
| Built with .NET + React (enterprise-ready stack) | Yes | Mostly Python/Streamlit | Various |

**Key differentiator:** No existing open-source project combines resume screening, auto-questionnaire generation, multi-round evaluation, and hiring recommendations in a single pipeline.

---

## Can You File a Patent?

### Short Answer
**Unlikely to get a strong software patent**, but there are strategies worth exploring. Here's the analysis:

### What's Patentable (Generally)
Patents require the invention to be:
1. **Novel** — not previously known or described
2. **Non-obvious** — not an obvious combination of known techniques
3. **Useful** — has practical application

### Challenges for This Project

1. **Abstract idea doctrine (Alice Corp. v. CLS Bank, 2014)**
   - US patent law heavily restricts "abstract ideas implemented on a computer"
   - "Rating resumes with AI" is likely considered an abstract business method
   - Using an LLM to evaluate text is a well-known technique

2. **Prior art is extensive**
   - 130+ open-source projects doing AI resume screening
   - Commercial products (HireVue, Ideal, etc.) have been doing this for years
   - Academic papers on ML-based resume evaluation date back to 2015+

3. **LLM-based evaluation is generic**
   - Sending a prompt to GPT-4o and parsing JSON responses is a widely known pattern
   - The specific prompts are creative but not patentable (they're instructions, not inventions)

### What *Might* Be Patentable

If you've developed a truly novel **method** (not just software), these aspects could be worth exploring:

| Potential Claim | Strength | Why |
|-----------------|----------|-----|
| Multi-round AI pipeline (screen → L1 → L2 → decision) | Medium | The specific orchestration of multiple AI evaluation stages is somewhat novel |
| 7-dimension scoring rubric with cross-candidate ranking | Weak | Scoring rubrics are known; the specific 7 dimensions aren't inventive enough |
| Auto-generating personalized interview questions from evaluation gaps | Medium | The feedback loop (evaluation → question generation → answer evaluation) is interesting |
| L2 challenge generation (4 types based on L1 results) | Medium | Adaptive challenge selection based on prior round results |
| Provider-agnostic AI evaluation architecture | Weak | Interface abstraction is standard software engineering |

### Recommendations

1. **Consider a provisional patent** ($320 fee) if you want to establish a priority date while you refine the invention. You have 12 months to file a full application.

2. **Focus on the method, not the software.** A patent on "a method for multi-stage AI-assisted candidate evaluation comprising..." has better chances than "a system that uses GPT-4o to rate resumes."

3. **Consult a patent attorney** specializing in software patents before investing. Budget $5,000-$15,000 for a full utility patent application.

4. **Alternative: Trade secret.** If your specific prompts and scoring methodology produce uniquely accurate results, keeping them proprietary (not open-source) may be more valuable than a patent.

5. **Alternative: First-mover advantage.** In the AI space, speed to market often matters more than patents. Ship the product, build a user base, and iterate faster than competitors.

### Relevant Patent Classes (If Proceeding)
- **CPC G06Q 10/1053** — Recruiting and hiring
- **CPC G06F 40/30** — Natural language processing
- **CPC G06N 20/00** — Machine learning
- **IPC G06Q 10/10** — Office automation, management of work

---

## Bottom Line

| Question | Answer |
|----------|--------|
| Are there similar projects? | Yes, 130+ open source + many commercial products |
| Is yours unique? | Yes — no one combines multi-round AI evaluation in a full pipeline |
| Can you patent it? | Difficult but not impossible; focus on the *method*, not the *software* |
| Best IP strategy? | Ship fast, build users, consider provisional patent for the pipeline method |
| Worth pursuing? | The market gap (full-pipeline AI hiring) is real and commercially valuable |
