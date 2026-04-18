import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5073/api',
  timeout: 120000, // 2 minutes to match backend AI timeout
});

export interface Resume {
  id: string;
  fileName: string;
  candidateName: string;
  linkedInUrl: string;
  gitHubUsername: string;
  uploadedAt: string;
}

export interface JobDescription {
  id: string;
  title: string;
  description: string;
  requiredSkills: string;
  preferredSkills: string;
  experienceLevel: string;
}

export interface CandidateEvaluation {
  id: string;
  resumeId: string;
  jobDescriptionId: string;
  candidateName: string;
  experienceScore: number;
  workHistoryScore: number;
  educationScore: number;
  sideProjectsScore: number;
  jobFitScore: number;
  awwFactorScore: number;
  uniquenessFactor: number;
  gitHubScore: number;
  onlinePresenceScore: number;
  codeProficiencyScore: number;
  resumeAuthenticityScore: number;
  buzzwordScore: number;
  aiGeneratedScore: number;
  overallScore: number;
  experienceFeedback: string;
  workHistoryFeedback: string;
  educationFeedback: string;
  sideProjectsFeedback: string;
  jobFitFeedback: string;
  awwFactorFeedback: string;
  uniquenessFeedback: string;
  gitHubFeedback: string;
  onlinePresenceFeedback: string;
  codeProficiencyFeedback: string;
  resumeAuthenticityFeedback: string;
  buzzwordFeedback: string;
  aiGeneratedFeedback: string;
  overallFeedback: string;
  tailoringRedFlags: string[];
  buzzwordsDetected: string[];
  authenticityAnalysis: string;
  standout: string;
  estimatedCurrentPackage: string;
  estimatedCurrentRole: string;
  expectedSalaryRange: string;
  recommendedForL1: boolean;
}

export interface Question {
  number: number;
  category: string;
  text: string;
  expectedInsight: string;
  difficulty: string;
}

export interface Questionnaire {
  id: string;
  evaluationId: string;
  candidateName: string;
  questions: Question[];
}

export interface AnswerEvaluation {
  questionNumber: number;
  question: string;
  candidateAnswer: string;
  score: number;
  feedback: string;
}

export interface L1Feedback {
  id: string;
  evaluationId: string;
  questionnaireId: string;
  candidateName: string;
  answerEvaluations: AnswerEvaluation[];
  overallL1Score: number;
  overallL1Feedback: string;
  techHandsOnRecommendation: string;
  areasToProbeInTechRound: string[];
  recommendedForTechRound: boolean;
}

// Resume APIs
export const uploadResume = (file: File, linkedInUrl?: string, gitHubUsername?: string) => {
  const formData = new FormData();
  formData.append('file', file);
  if (linkedInUrl) formData.append('linkedInUrl', linkedInUrl);
  if (gitHubUsername) formData.append('gitHubUsername', gitHubUsername);
  return api.post<Resume>('/resume/upload', formData);
};

export const getResumes = () => api.get<Resume[]>('/resume');

export const deleteResume = (resumeId: string) => api.delete(`/resume/${resumeId}`);

// Job Description APIs
export const createJobDescription = (jd: Omit<JobDescription, 'id'>) =>
  api.post<JobDescription>('/jobdescription', jd);

export const getJobDescriptions = () => api.get<JobDescription[]>('/jobdescription');

export const deleteJobDescription = (jobDescriptionId: string) =>
  api.delete(`/jobdescription/${jobDescriptionId}`);

// Evaluation APIs
export const evaluateResume = (resumeId: string, jobDescriptionId: string) =>
  api.post<CandidateEvaluation>('/evaluation/evaluate', { resumeId, jobDescriptionId });

export const getEvaluations = (jobDescriptionId: string) =>
  api.get<CandidateEvaluation[]>(`/evaluation/${jobDescriptionId}`);

export const deleteEvaluation = (evaluationId: string) =>
  api.delete(`/evaluation/${evaluationId}`);

// SSE streaming evaluate
export interface EvalProgress {
  step: string;
  message?: string;
  progress?: number;
  evaluation?: CandidateEvaluation;
}

export const evaluateResumeStream = (
  resumeId: string,
  jobDescriptionId: string,
  onProgress: (evt: EvalProgress) => void,
): Promise<CandidateEvaluation | null> => {
  return new Promise((resolve, reject) => {
    fetch('http://localhost:5073/api/evaluation/evaluate-stream', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ resumeId, jobDescriptionId }),
    })
      .then(response => {
        const reader = response.body?.getReader();
        const decoder = new TextDecoder();
        if (!reader) { reject(new Error('No reader')); return; }

        let buffer = '';
        let resolved = false;
        const processBuffer = () => {
          const lines = buffer.split('\n\n');
          buffer = lines.pop() || '';
          for (const line of lines) {
            if (line.startsWith('data: ')) {
              try {
                const data = JSON.parse(line.slice(6)) as EvalProgress;
                onProgress(data);
                if (data.step === 'result' && data.evaluation) {
                  resolved = true;
                  resolve(data.evaluation);
                }
              } catch { /* ignore parse errors */ }
            }
          }
        };
        const read = (): Promise<void> => reader.read().then(({ done, value }) => {
          if (done) {
            // Process any remaining data in the buffer
            if (buffer.trim()) {
              buffer += '\n\n';
              processBuffer();
            }
            if (!resolved) resolve(null);
            return;
          }
          buffer += decoder.decode(value, { stream: true });
          processBuffer();
          return read();
        });
        read().catch(reject);
      })
      .catch(reject);
  });
};

export const generateQuestionnaire = (evaluationId: string) =>
  api.post<Questionnaire>(`/evaluation/questionnaire/${evaluationId}`);

export const submitL1Answers = (request: {
  evaluationId: string;
  questionnaireId: string;
  answers: { questionNumber: number; answer: string }[];
}) => api.post<L1Feedback>('/evaluation/l1-feedback', request);

// Seed API
export interface SeedResult {
  resumesLoaded: number;
  jobDescriptionId: string | null;
  jobDescriptionTitle: string | null;
  errors: string[];
}

export const seedFromAssets = () => api.post<SeedResult>('/evaluation/seed');

// Evaluate All API
export const evaluateAll = (jobDescriptionId: string) =>
  api.post<CandidateEvaluation[]>(`/evaluation/evaluate-all/${jobDescriptionId}`);

// L2 Round APIs
export interface L2ChallengePrompt {
  scenario: string;
  expectedApproach: string;
  evaluationCriteria: string[];
}

export interface L2Questionnaire {
  id: string;
  evaluationId: string;
  l1FeedbackId: string;
  candidateName: string;
  systemDesign: L2ChallengePrompt;
  handsOnCoding: L2ChallengePrompt;
  designThinking: L2ChallengePrompt;
  tradeOffAnalysis: L2ChallengePrompt;
}

export interface L2Challenge {
  scenario: string;
  expectedApproach: string;
  evaluationCriteria: string[];
  candidateResponse: string;
  score: number;
  feedback: string;
}

export interface L2Assessment {
  id: string;
  evaluationId: string;
  l1FeedbackId: string;
  candidateName: string;
  systemDesign: L2Challenge;
  handsOnCoding: L2Challenge;
  designThinking: L2Challenge;
  tradeOffAnalysis: L2Challenge;
  overallL2Score: number;
  overallL2Feedback: string;
  strengths: string;
  weaknesses: string;
  hiringRecommendation: string;
  recommendedForHire: boolean;
}

export const generateL2Questionnaire = (evaluationId: string) =>
  api.post<L2Questionnaire>(`/evaluation/l2-questionnaire/${evaluationId}`);

export const submitL2Answers = (request: {
  evaluationId: string;
  l2QuestionnaireId: string;
  systemDesignAnswer: string;
  handsOnCodingAnswer: string;
  designThinkingAnswer: string;
  tradeOffAnalysisAnswer: string;
}) => api.post<L2Assessment>('/evaluation/l2-feedback', request);
