### Refined Prompt

#### Project Overview

1. Develop an application that:
   - Reads data from uploaded resumes (PDFs stored in the `assets` folder or a designated `resume` folder).
   - Rates profiles based on the provided job description (stored in an `.md` file).
   - Evaluation criteria include:
     - Experience
     - Work history
     - Education
     - Side projects
     - Fit for the job description
     - Unique factors that differentiate the candidate
     - "Aww" factor or standout qualities.

#### Features and Functionalities

2. **Evaluation Process**:
   - For L1 round:
     - Prepare a custom questionnaire for shortlisted candidates.
     - Rate candidates based on their responses and provide detailed feedback for the technical hands-on round.
   - For L2 round:
     - Evaluate candidates on system design, design thinking, and trade-off use cases.
     - Include HR and leadership rounds for a holistic assessment.

2. **Additional Evaluation Inputs**:
   - Integrate LinkedIn profile analysis for authenticity verification.
   - Analyze GitHub contributions, focusing on project codebases relevant to the job description, code logic, innovation, and proficiency.
   - Address scenarios where resumes are tailored to fit job descriptions by implementing checks for authenticity and consistency.

3. **Comparison and Reporting**:
   - Provide a comparative analysis of all uploaded profiles, not just individual evaluations.
   - Include a feature to delete specific candidates or job descriptions from the evaluation process.
   - Group profiles based on the selected job description.

4. **UI Enhancements**:
   - Add validation to disable the "Evaluate" button until a job description is selected.
   - Display intermediate results during the evaluation process (e.g., LinkedIn check, GitHub analysis) with a progress bar.
   - Handle duplicate resumes and update logic accordingly.

5. **Data Management**:
   - Enable uploading and saving of resumes and job descriptions to a document database.
   - Store evaluation details and results within the repository for future reference.

6. **Cost Optimization**:
   - Create a `COST_OPTIMIZATION.md` file detailing:
     - Number of tokens used.
     - Exact prompts utilized.
     - Areas where internal logic can replace AI to reduce costs.
     - Time taken for each API call.

7. **Documentation**:
   - Add the following files to the repository:
     - `README.md` for project overview and setup instructions.
     - `CHANGELOG.md` for tracking updates.
     - `CONTRIBUTING.md` for contribution guidelines.
     - `ARCHITECTURE.md` for system design and architecture details.
     - `ENHANCEMENTS.md` with 10 improvement areas, top 5 priorities, feedback, and usability analysis.
     - `TESTING.md` for application testing results as a test architect.
     - `PROMPT_FEEDBACK.md` for feedback on prompts and areas for improvement.

8. **Patent and Similar Projects**:
   - Research similar projects (e.g., <https://github.com/topics/resume-screening>) and include findings in the documentation.
   - Assess the feasibility of filing a patent for the application.

#### Output Requirements

- Provide detailed feedback for each candidate after evaluation.
- Estimate the candidate's current package, role, and expected salary.
- Include all results and analyses in the respective `.md` files for documentation.
- Ensure the application is integrated with OpenAPI and supports Anthropic OpenAI API keys.
