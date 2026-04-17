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
  overallFeedback: string;
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

// Job Description APIs
export const createJobDescription = (jd: Omit<JobDescription, 'id'>) =>
  api.post<JobDescription>('/jobdescription', jd);

export const getJobDescriptions = () => api.get<JobDescription[]>('/jobdescription');

// Evaluation APIs
export const evaluateResume = (resumeId: string, jobDescriptionId: string) =>
  api.post<CandidateEvaluation>('/evaluation/evaluate', { resumeId, jobDescriptionId });

export const getEvaluations = (jobDescriptionId: string) =>
  api.get<CandidateEvaluation[]>(`/evaluation/${jobDescriptionId}`);

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
