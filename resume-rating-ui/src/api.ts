import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5073/api',
});

export interface Resume {
  id: string;
  fileName: string;
  candidateName: string;
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
  overallScore: number;
  experienceFeedback: string;
  workHistoryFeedback: string;
  educationFeedback: string;
  sideProjectsFeedback: string;
  jobFitFeedback: string;
  awwFactorFeedback: string;
  uniquenessFeedback: string;
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
export const uploadResume = (file: File) => {
  const formData = new FormData();
  formData.append('file', file);
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
