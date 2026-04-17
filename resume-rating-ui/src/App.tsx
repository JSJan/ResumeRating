import React, { useState } from 'react';
import {
  Resume, JobDescription, CandidateEvaluation, Questionnaire, L1Feedback,
  L2Questionnaire, L2Assessment,
  uploadResume, getResumes, createJobDescription, getJobDescriptions,
  evaluateResume, getEvaluations, generateQuestionnaire, submitL1Answers,
  seedFromAssets, evaluateAll, generateL2Questionnaire, submitL2Answers
} from './api';

type Tab = 'upload' | 'jobDescription' | 'evaluate' | 'comparison' | 'questionnaire' | 'l1feedback' | 'l2round' | 'l2feedback';

const App: React.FC = () => {
  const [activeTab, setActiveTab] = useState<Tab>('upload');
  const [resumes, setResumes] = useState<Resume[]>([]);
  const [jobDescs, setJobDescs] = useState<JobDescription[]>([]);
  const [evaluations, setEvaluations] = useState<CandidateEvaluation[]>([]);
  const [questionnaire, setQuestionnaire] = useState<Questionnaire | null>(null);
  const [l1Feedback, setL1Feedback] = useState<L1Feedback | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  // Upload state
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [linkedInUrl, setLinkedInUrl] = useState('');
  const [gitHubUsername, setGitHubUsername] = useState('');

  // JD state
  const [jdForm, setJdForm] = useState({ title: '', description: '', requiredSkills: '', preferredSkills: '', experienceLevel: '' });

  // Evaluate state
  const [selectedResume, setSelectedResume] = useState('');
  const [selectedJd, setSelectedJd] = useState('');
  const [currentEvaluation, setCurrentEvaluation] = useState<CandidateEvaluation | null>(null);

  // L1 answers state
  const [answers, setAnswers] = useState<Record<number, string>>({});

  // Comparison state
  const [comparisonJd, setComparisonJd] = useState('');
  const [comparisonEvals, setComparisonEvals] = useState<CandidateEvaluation[]>([]);
  const [sortField, setSortField] = useState<'overallScore' | 'experienceScore' | 'workHistoryScore' | 'educationScore' | 'sideProjectsScore' | 'jobFitScore' | 'awwFactorScore' | 'uniquenessFactor'>('overallScore');
  const [sortAsc, setSortAsc] = useState(false);

  // L2 state
  const [l2Questionnaire, setL2Questionnaire] = useState<L2Questionnaire | null>(null);
  const [l2Assessment, setL2Assessment] = useState<L2Assessment | null>(null);
  const [l2Answers, setL2Answers] = useState({ systemDesign: '', handsOnCoding: '', designThinking: '', tradeOffAnalysis: '' });

  const handleError = (err: unknown) => {
    if (err instanceof Error) setError(err.message);
    else setError('An error occurred');
  };

  const handleUpload = async () => {
    if (!selectedFile) return;
    setLoading(true);
    setError('');
    try {
      await uploadResume(selectedFile, linkedInUrl || undefined, gitHubUsername || undefined);
      const res = await getResumes();
      setResumes(res.data);
      setSelectedFile(null);
      setLinkedInUrl('');
      setGitHubUsername('');
    } catch (e) { handleError(e); }
    setLoading(false);
  };

  const handleCreateJd = async () => {
    if (!jdForm.title || !jdForm.description) return;
    setLoading(true);
    setError('');
    try {
      await createJobDescription(jdForm);
      const res = await getJobDescriptions();
      setJobDescs(res.data);
      setJdForm({ title: '', description: '', requiredSkills: '', preferredSkills: '', experienceLevel: '' });
    } catch (e) { handleError(e); }
    setLoading(false);
  };

  const handleEvaluate = async () => {
    if (!selectedResume || !selectedJd) return;
    setLoading(true);
    setError('');
    try {
      const res = await evaluateResume(selectedResume, selectedJd);
      setCurrentEvaluation(res.data);
      const evals = await getEvaluations(selectedJd);
      setEvaluations(evals.data);
    } catch (e) { handleError(e); }
    setLoading(false);
  };

  const handleGenerateQuestionnaire = async (evalId: string) => {
    setLoading(true);
    setError('');
    try {
      const res = await generateQuestionnaire(evalId);
      setQuestionnaire(res.data);
      setAnswers({});
      setActiveTab('questionnaire');
    } catch (e) { handleError(e); }
    setLoading(false);
  };

  const handleSubmitAnswers = async () => {
    if (!questionnaire || !currentEvaluation) return;
    setLoading(true);
    setError('');
    try {
      const res = await submitL1Answers({
        evaluationId: currentEvaluation.id,
        questionnaireId: questionnaire.id,
        answers: questionnaire.questions.map(q => ({
          questionNumber: q.number,
          answer: answers[q.number] || ''
        }))
      });
      setL1Feedback(res.data);
      setActiveTab('l1feedback');
    } catch (e) { handleError(e); }
    setLoading(false);
  };

  const handleSeed = async () => {
    setLoading(true);
    setError('');
    try {
      const res = await seedFromAssets();
      if (res.data.errors.length > 0) {
        setError(`Loaded with warnings: ${res.data.errors.join(', ')}`);
      }
      const [r, j] = await Promise.all([getResumes(), getJobDescriptions()]);
      setResumes(r.data);
      setJobDescs(j.data);
    } catch (e) { handleError(e); }
    setLoading(false);
  };

  const loadData = async () => {
    try {
      const [r, j] = await Promise.all([getResumes(), getJobDescriptions()]);
      setResumes(r.data);
      setJobDescs(j.data);
    } catch { /* ignore initial load errors */ }
  };

  React.useEffect(() => { loadData(); }, []);

  const handleLoadComparison = async () => {
    if (!comparisonJd) return;
    setLoading(true);
    setError('');
    try {
      const res = await getEvaluations(comparisonJd);
      setComparisonEvals(res.data);
      if (res.data.length === 0) setError('No evaluations found for this job description. Evaluate candidates first or click "Evaluate All".');
    } catch (e) { handleError(e); }
    setLoading(false);
  };

  const handleEvaluateAll = async () => {
    if (!comparisonJd) return;
    setLoading(true);
    setError('');
    try {
      const res = await evaluateAll(comparisonJd);
      setComparisonEvals(res.data);
    } catch (e) { handleError(e); }
    setLoading(false);
  };

  const handleGenerateL2 = async (evalId: string) => {
    setLoading(true);
    setError('');
    try {
      const res = await generateL2Questionnaire(evalId);
      setL2Questionnaire(res.data);
      setL2Answers({ systemDesign: '', handsOnCoding: '', designThinking: '', tradeOffAnalysis: '' });
      setActiveTab('l2round');
    } catch (e) { handleError(e); }
    setLoading(false);
  };

  const handleSubmitL2 = async () => {
    if (!l2Questionnaire || !currentEvaluation) return;
    setLoading(true);
    setError('');
    try {
      const res = await submitL2Answers({
        evaluationId: currentEvaluation.id,
        l2QuestionnaireId: l2Questionnaire.id,
        systemDesignAnswer: l2Answers.systemDesign,
        handsOnCodingAnswer: l2Answers.handsOnCoding,
        designThinkingAnswer: l2Answers.designThinking,
        tradeOffAnalysisAnswer: l2Answers.tradeOffAnalysis,
      });
      setL2Assessment(res.data);
      setActiveTab('l2feedback');
    } catch (e) { handleError(e); }
    setLoading(false);
  };

  const sortedComparisonEvals = [...comparisonEvals].sort((a, b) => {
    const diff = (a[sortField] as number) - (b[sortField] as number);
    return sortAsc ? diff : -diff;
  });

  const handleSortToggle = (field: typeof sortField) => {
    if (sortField === field) setSortAsc(!sortAsc);
    else { setSortField(field); setSortAsc(false); }
  };

  const scoreColor = (s: number) => s >= 7 ? '#22c55e' : s >= 4 ? '#f59e0b' : '#ef4444';

  const ScoreBar: React.FC<{ label: string; score: number; feedback: string }> = ({ label, score, feedback }) => (
    <div style={{ marginBottom: 12 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
        <strong>{label}</strong>
        <span style={{ color: score >= 7 ? '#22c55e' : score >= 4 ? '#f59e0b' : '#ef4444', fontWeight: 'bold' }}>
          {score}/10
        </span>
      </div>
      <div style={{ background: '#e5e7eb', borderRadius: 8, height: 8 }}>
        <div style={{ width: `${score * 10}%`, background: score >= 7 ? '#22c55e' : score >= 4 ? '#f59e0b' : '#ef4444', borderRadius: 8, height: 8 }} />
      </div>
      <p style={{ color: '#6b7280', fontSize: 14, marginTop: 4 }}>{feedback}</p>
    </div>
  );

  return (
    <div style={{ maxWidth: 960, margin: '0 auto', padding: 24, fontFamily: 'system-ui, sans-serif' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
        <h1 style={{ fontSize: 28, fontWeight: 'bold', margin: 0 }}>Resume Rating System</h1>
        <button onClick={handleSeed} disabled={loading}
          style={{ background: '#8b5cf6', color: '#fff', padding: '8px 16px', border: 'none', borderRadius: 8, cursor: 'pointer', opacity: loading ? 0.5 : 1, fontSize: 14 }}>
          {loading ? 'Loading...' : 'Load Sample Data'}
        </button>
      </div>
      <p style={{ color: '#6b7280', marginBottom: 24 }}>AI-powered resume evaluation and interview preparation</p>

      {error && <div style={{ background: '#fef2f2', border: '1px solid #fecaca', padding: 12, borderRadius: 8, color: '#dc2626', marginBottom: 16 }}>{error}</div>}

      {/* Tabs */}
      <div style={{ display: 'flex', gap: 4, marginBottom: 24, borderBottom: '2px solid #e5e7eb', flexWrap: 'wrap' }}>
        {([['upload', 'Upload Resumes'], ['jobDescription', 'Job Description'], ['evaluate', 'Evaluate'], ['comparison', 'Compare All'], ['questionnaire', 'L1 Questionnaire'], ['l1feedback', 'L1 Feedback'], ['l2round', 'L2 Tech Round'], ['l2feedback', 'L2 Result']] as [Tab, string][]).map(([key, label]) => (
          <button
            key={key}
            onClick={() => setActiveTab(key)}
            style={{
              padding: '10px 20px', border: 'none', cursor: 'pointer',
              background: activeTab === key ? '#3b82f6' : 'transparent',
              color: activeTab === key ? '#fff' : '#6b7280',
              borderRadius: '8px 8px 0 0', fontWeight: activeTab === key ? 'bold' : 'normal'
            }}
          >
            {label}
          </button>
        ))}
      </div>

      {/* Upload Tab */}
      {activeTab === 'upload' && (
        <div>
          <h2 style={{ fontSize: 20, marginBottom: 16 }}>Upload Resumes</h2>
          <div style={{ border: '2px dashed #d1d5db', borderRadius: 8, padding: 32, textAlign: 'center', marginBottom: 16 }}>
            <input type="file" accept=".pdf,.docx,.txt" onChange={e => setSelectedFile(e.target.files?.[0] || null)} />
            <p style={{ color: '#9ca3af', marginTop: 8 }}>Supported: PDF, DOCX, TXT (max 10MB)</p>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, marginBottom: 16 }}>
            <input placeholder="LinkedIn URL (optional)" value={linkedInUrl} onChange={e => setLinkedInUrl(e.target.value)}
              style={{ padding: 10, border: '1px solid #d1d5db', borderRadius: 8 }} />
            <input placeholder="GitHub Username (optional)" value={gitHubUsername} onChange={e => setGitHubUsername(e.target.value)}
              style={{ padding: 10, border: '1px solid #d1d5db', borderRadius: 8 }} />
          </div>
          <button onClick={handleUpload} disabled={!selectedFile || loading}
            style={{ background: '#3b82f6', color: '#fff', padding: '10px 24px', border: 'none', borderRadius: 8, cursor: 'pointer', opacity: !selectedFile || loading ? 0.5 : 1 }}>
            {loading ? 'Uploading...' : 'Upload Resume'}
          </button>

          {resumes.length > 0 && (
            <div style={{ marginTop: 24 }}>
              <h3 style={{ fontSize: 16, marginBottom: 8 }}>Uploaded Resumes ({resumes.length})</h3>
              <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                <thead>
                  <tr style={{ background: '#f9fafb' }}>
                    <th style={{ padding: 8, textAlign: 'left', borderBottom: '1px solid #e5e7eb' }}>Name</th>
                    <th style={{ padding: 8, textAlign: 'left', borderBottom: '1px solid #e5e7eb' }}>File</th>
                    <th style={{ padding: 8, textAlign: 'left', borderBottom: '1px solid #e5e7eb' }}>LinkedIn</th>
                    <th style={{ padding: 8, textAlign: 'left', borderBottom: '1px solid #e5e7eb' }}>GitHub</th>
                    <th style={{ padding: 8, textAlign: 'left', borderBottom: '1px solid #e5e7eb' }}>Uploaded</th>
                  </tr>
                </thead>
                <tbody>
                  {resumes.map(r => (
                    <tr key={r.id}>
                      <td style={{ padding: 8, borderBottom: '1px solid #e5e7eb' }}>{r.candidateName}</td>
                      <td style={{ padding: 8, borderBottom: '1px solid #e5e7eb' }}>{r.fileName}</td>
                      <td style={{ padding: 8, borderBottom: '1px solid #e5e7eb' }}>
                        {r.linkedInUrl ? <a href={r.linkedInUrl} target="_blank" rel="noopener noreferrer" style={{ color: '#0077b5' }}>Profile</a> : <span style={{ color: '#9ca3af' }}>—</span>}
                      </td>
                      <td style={{ padding: 8, borderBottom: '1px solid #e5e7eb' }}>
                        {r.gitHubUsername ? <a href={`https://github.com/${r.gitHubUsername}`} target="_blank" rel="noopener noreferrer" style={{ color: '#333' }}>{r.gitHubUsername}</a> : <span style={{ color: '#9ca3af' }}>—</span>}
                      </td>
                      <td style={{ padding: 8, borderBottom: '1px solid #e5e7eb' }}>{new Date(r.uploadedAt).toLocaleDateString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* Job Description Tab */}
      {activeTab === 'jobDescription' && (
        <div>
          <h2 style={{ fontSize: 20, marginBottom: 16 }}>Create Job Description</h2>
          <div style={{ display: 'grid', gap: 12 }}>
            <input placeholder="Job Title *" value={jdForm.title} onChange={e => setJdForm({ ...jdForm, title: e.target.value })}
              style={{ padding: 10, border: '1px solid #d1d5db', borderRadius: 8 }} />
            <textarea placeholder="Job Description *" rows={4} value={jdForm.description} onChange={e => setJdForm({ ...jdForm, description: e.target.value })}
              style={{ padding: 10, border: '1px solid #d1d5db', borderRadius: 8 }} />
            <input placeholder="Required Skills (comma-separated)" value={jdForm.requiredSkills} onChange={e => setJdForm({ ...jdForm, requiredSkills: e.target.value })}
              style={{ padding: 10, border: '1px solid #d1d5db', borderRadius: 8 }} />
            <input placeholder="Preferred Skills (comma-separated)" value={jdForm.preferredSkills} onChange={e => setJdForm({ ...jdForm, preferredSkills: e.target.value })}
              style={{ padding: 10, border: '1px solid #d1d5db', borderRadius: 8 }} />
            <input placeholder="Experience Level (e.g., 3-5 years)" value={jdForm.experienceLevel} onChange={e => setJdForm({ ...jdForm, experienceLevel: e.target.value })}
              style={{ padding: 10, border: '1px solid #d1d5db', borderRadius: 8 }} />
            <button onClick={handleCreateJd} disabled={loading || !jdForm.title || !jdForm.description}
              style={{ background: '#3b82f6', color: '#fff', padding: '10px 24px', border: 'none', borderRadius: 8, cursor: 'pointer', opacity: loading ? 0.5 : 1 }}>
              {loading ? 'Creating...' : 'Create Job Description'}
            </button>
          </div>

          {jobDescs.length > 0 && (
            <div style={{ marginTop: 24 }}>
              <h3 style={{ fontSize: 16, marginBottom: 8 }}>Existing Job Descriptions</h3>
              {jobDescs.map(jd => (
                <div key={jd.id} style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 12, marginBottom: 8 }}>
                  <strong>{jd.title}</strong>
                  <p style={{ color: '#6b7280', fontSize: 14 }}>{jd.description.substring(0, 150)}...</p>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Evaluate Tab */}
      {activeTab === 'evaluate' && (
        <div>
          <h2 style={{ fontSize: 20, marginBottom: 16 }}>Evaluate Candidates</h2>
          <div style={{ display: 'grid', gap: 12, marginBottom: 16 }}>
            <select value={selectedResume} onChange={e => setSelectedResume(e.target.value)}
              style={{ padding: 10, border: '1px solid #d1d5db', borderRadius: 8 }}>
              <option value="">Select Resume</option>
              {resumes.map(r => <option key={r.id} value={r.id}>{r.candidateName} - {r.fileName}</option>)}
            </select>
            <select value={selectedJd} onChange={e => setSelectedJd(e.target.value)}
              style={{ padding: 10, border: '1px solid #d1d5db', borderRadius: 8 }}>
              <option value="">Select Job Description</option>
              {jobDescs.map(jd => <option key={jd.id} value={jd.id}>{jd.title}</option>)}
            </select>
            <button onClick={handleEvaluate} disabled={loading || !selectedResume || !selectedJd}
              style={{ background: '#3b82f6', color: '#fff', padding: '10px 24px', border: 'none', borderRadius: 8, cursor: 'pointer', opacity: loading ? 0.5 : 1 }}>
              {loading ? 'Evaluating with AI...' : 'Evaluate Resume'}
            </button>
          </div>

          {currentEvaluation && (
            <div style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 20 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
                <h3 style={{ fontSize: 18 }}>{currentEvaluation.candidateName}</h3>
                <span style={{
                  fontSize: 24, fontWeight: 'bold',
                  color: currentEvaluation.overallScore >= 7 ? '#22c55e' : currentEvaluation.overallScore >= 4 ? '#f59e0b' : '#ef4444'
                }}>
                  {currentEvaluation.overallScore}/10
                </span>
              </div>

              <ScoreBar label="Experience" score={currentEvaluation.experienceScore} feedback={currentEvaluation.experienceFeedback} />
              <ScoreBar label="Work History" score={currentEvaluation.workHistoryScore} feedback={currentEvaluation.workHistoryFeedback} />
              <ScoreBar label="Education" score={currentEvaluation.educationScore} feedback={currentEvaluation.educationFeedback} />
              <ScoreBar label="Side Projects" score={currentEvaluation.sideProjectsScore} feedback={currentEvaluation.sideProjectsFeedback} />
              <ScoreBar label="Job Fit" score={currentEvaluation.jobFitScore} feedback={currentEvaluation.jobFitFeedback} />
              <ScoreBar label="Aww Factor" score={currentEvaluation.awwFactorScore} feedback={currentEvaluation.awwFactorFeedback} />
              <ScoreBar label="Uniqueness" score={currentEvaluation.uniquenessFactor} feedback={currentEvaluation.uniquenessFeedback} />
              <ScoreBar label="GitHub Activity" score={currentEvaluation.gitHubScore} feedback={currentEvaluation.gitHubFeedback} />
              <ScoreBar label="Online Presence" score={currentEvaluation.onlinePresenceScore} feedback={currentEvaluation.onlinePresenceFeedback} />

              <div style={{ background: '#f9fafb', borderRadius: 8, padding: 16, marginTop: 16 }}>
                <h4>What Sets Them Apart</h4>
                <p>{currentEvaluation.standout}</p>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 12, marginTop: 16 }}>
                <div style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 12 }}>
                  <small style={{ color: '#9ca3af' }}>Estimated Current Package</small>
                  <p style={{ fontWeight: 'bold' }}>{currentEvaluation.estimatedCurrentPackage}</p>
                </div>
                <div style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 12 }}>
                  <small style={{ color: '#9ca3af' }}>Current Role</small>
                  <p style={{ fontWeight: 'bold' }}>{currentEvaluation.estimatedCurrentRole}</p>
                </div>
                <div style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 12 }}>
                  <small style={{ color: '#9ca3af' }}>Expected Salary</small>
                  <p style={{ fontWeight: 'bold' }}>{currentEvaluation.expectedSalaryRange}</p>
                </div>
              </div>

              {currentEvaluation.recommendedForL1 && (
                <button onClick={() => handleGenerateQuestionnaire(currentEvaluation.id)} disabled={loading}
                  style={{ marginTop: 16, background: '#22c55e', color: '#fff', padding: '10px 24px', border: 'none', borderRadius: 8, cursor: 'pointer' }}>
                  {loading ? 'Generating...' : 'Generate L1 Questionnaire'}
                </button>
              )}
              {!currentEvaluation.recommendedForL1 && (
                <p style={{ marginTop: 16, color: '#ef4444', fontWeight: 'bold' }}>Not recommended for L1 round</p>
              )}
            </div>
          )}
        </div>
      )}

      {/* Comparison Tab */}
      {activeTab === 'comparison' && (
        <div>
          <h2 style={{ fontSize: 20, marginBottom: 16 }}>Compare All Candidates</h2>
          <div style={{ display: 'flex', gap: 12, marginBottom: 16, alignItems: 'center' }}>
            <select value={comparisonJd} onChange={e => setComparisonJd(e.target.value)}
              style={{ padding: 10, border: '1px solid #d1d5db', borderRadius: 8, flex: 1 }}>
              <option value="">Select Job Description</option>
              {jobDescs.map(jd => <option key={jd.id} value={jd.id}>{jd.title}</option>)}
            </select>
            <button onClick={handleLoadComparison} disabled={loading || !comparisonJd}
              style={{ background: '#3b82f6', color: '#fff', padding: '10px 24px', border: 'none', borderRadius: 8, cursor: 'pointer', opacity: loading || !comparisonJd ? 0.5 : 1, whiteSpace: 'nowrap' }}>
              {loading ? 'Loading...' : 'Load Comparison'}
            </button>
            <button onClick={handleEvaluateAll} disabled={loading || !comparisonJd}
              style={{ background: '#8b5cf6', color: '#fff', padding: '10px 24px', border: 'none', borderRadius: 8, cursor: 'pointer', opacity: loading || !comparisonJd ? 0.5 : 1, whiteSpace: 'nowrap' }}>
              {loading ? 'Evaluating...' : 'Evaluate All & Compare'}
            </button>
          </div>

          {sortedComparisonEvals.length > 0 && (
            <>
              {/* Summary cards */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr 1fr', gap: 12, marginBottom: 20 }}>
                <div style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 12, textAlign: 'center' }}>
                  <small style={{ color: '#9ca3af' }}>Total Evaluated</small>
                  <p style={{ fontSize: 24, fontWeight: 'bold', margin: 4 }}>{comparisonEvals.length}</p>
                </div>
                <div style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 12, textAlign: 'center' }}>
                  <small style={{ color: '#9ca3af' }}>Recommended for L1</small>
                  <p style={{ fontSize: 24, fontWeight: 'bold', margin: 4, color: '#22c55e' }}>{comparisonEvals.filter(e => e.recommendedForL1).length}</p>
                </div>
                <div style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 12, textAlign: 'center' }}>
                  <small style={{ color: '#9ca3af' }}>Avg Overall Score</small>
                  <p style={{ fontSize: 24, fontWeight: 'bold', margin: 4 }}>
                    {(comparisonEvals.reduce((s, e) => s + e.overallScore, 0) / comparisonEvals.length).toFixed(1)}
                  </p>
                </div>
                <div style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 12, textAlign: 'center' }}>
                  <small style={{ color: '#9ca3af' }}>Top Scorer</small>
                  <p style={{ fontSize: 14, fontWeight: 'bold', margin: 4, color: '#3b82f6' }}>
                    {[...comparisonEvals].sort((a, b) => b.overallScore - a.overallScore)[0]?.candidateName}
                  </p>
                </div>
              </div>

              {/* Comparison table */}
              <div style={{ overflowX: 'auto' }}>
                <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 14 }}>
                  <thead>
                    <tr style={{ background: '#f9fafb' }}>
                      <th style={{ padding: 10, textAlign: 'left', borderBottom: '2px solid #e5e7eb' }}>#</th>
                      <th style={{ padding: 10, textAlign: 'left', borderBottom: '2px solid #e5e7eb' }}>Candidate</th>
                      {([
                        ['overallScore', 'Overall'],
                        ['experienceScore', 'Experience'],
                        ['workHistoryScore', 'Work History'],
                        ['educationScore', 'Education'],
                        ['sideProjectsScore', 'Projects'],
                        ['jobFitScore', 'Job Fit'],
                        ['awwFactorScore', 'Aww Factor'],
                        ['uniquenessFactor', 'Unique'],
                      ] as [typeof sortField, string][]).map(([field, label]) => (
                        <th key={field}
                          onClick={() => handleSortToggle(field)}
                          style={{ padding: 10, textAlign: 'center', borderBottom: '2px solid #e5e7eb', cursor: 'pointer', userSelect: 'none', whiteSpace: 'nowrap' }}>
                          {label} {sortField === field ? (sortAsc ? '▲' : '▼') : ''}
                        </th>
                      ))}
                      <th style={{ padding: 10, textAlign: 'center', borderBottom: '2px solid #e5e7eb' }}>L1?</th>
                      <th style={{ padding: 10, textAlign: 'left', borderBottom: '2px solid #e5e7eb' }}>Expected Salary</th>
                    </tr>
                  </thead>
                  <tbody>
                    {sortedComparisonEvals.map((ev, idx) => (
                      <tr key={ev.id} style={{ background: idx % 2 === 0 ? '#fff' : '#f9fafb' }}>
                        <td style={{ padding: 10, borderBottom: '1px solid #e5e7eb', fontWeight: 'bold', color: '#9ca3af' }}>{idx + 1}</td>
                        <td style={{ padding: 10, borderBottom: '1px solid #e5e7eb', fontWeight: 'bold', whiteSpace: 'nowrap' }}>{ev.candidateName}</td>
                        {([ev.overallScore, ev.experienceScore, ev.workHistoryScore, ev.educationScore, ev.sideProjectsScore, ev.jobFitScore, ev.awwFactorScore, ev.uniquenessFactor]).map((score, i) => (
                          <td key={i} style={{ padding: 10, borderBottom: '1px solid #e5e7eb', textAlign: 'center', fontWeight: i === 0 ? 'bold' : 'normal', color: scoreColor(score) }}>
                            {score}/10
                          </td>
                        ))}
                        <td style={{ padding: 10, borderBottom: '1px solid #e5e7eb', textAlign: 'center' }}>
                          {ev.recommendedForL1
                            ? <span style={{ color: '#22c55e', fontWeight: 'bold' }}>✓ Yes</span>
                            : <span style={{ color: '#ef4444' }}>✗ No</span>}
                        </td>
                        <td style={{ padding: 10, borderBottom: '1px solid #e5e7eb', whiteSpace: 'nowrap' }}>{ev.expectedSalaryRange}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {/* Individual standouts */}
              <h3 style={{ fontSize: 16, marginTop: 24, marginBottom: 12 }}>Candidate Highlights</h3>
              <div style={{ display: 'grid', gap: 12 }}>
                {[...comparisonEvals].sort((a, b) => b.overallScore - a.overallScore).map((ev, idx) => (
                  <div key={ev.id} style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 16, display: 'flex', gap: 16, alignItems: 'flex-start' }}>
                    <div style={{
                      width: 40, height: 40, borderRadius: '50%', display: 'flex', alignItems: 'center', justifyContent: 'center',
                      background: idx === 0 ? '#fef3c7' : idx === 1 ? '#f3f4f6' : idx === 2 ? '#fed7aa' : '#f9fafb',
                      fontWeight: 'bold', fontSize: 16, flexShrink: 0,
                      color: idx === 0 ? '#b45309' : idx === 1 ? '#6b7280' : idx === 2 ? '#c2410c' : '#9ca3af'
                    }}>
                      {idx + 1}
                    </div>
                    <div style={{ flex: 1 }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
                        <strong>{ev.candidateName}</strong>
                        <span style={{ fontWeight: 'bold', color: scoreColor(ev.overallScore) }}>{ev.overallScore}/10</span>
                      </div>
                      <p style={{ color: '#374151', fontSize: 14, margin: '4px 0' }}><strong>Role:</strong> {ev.estimatedCurrentRole} | <strong>Current Pkg:</strong> {ev.estimatedCurrentPackage}</p>
                      <p style={{ color: '#6b7280', fontSize: 14, margin: '4px 0' }}>{ev.standout}</p>
                    </div>
                  </div>
                ))}
              </div>
            </>
          )}
        </div>
      )}

      {/* Questionnaire Tab */}
      {activeTab === 'questionnaire' && questionnaire && (
        <div>
          <h2 style={{ fontSize: 20, marginBottom: 16 }}>L1 Questionnaire - {questionnaire.candidateName}</h2>
          {questionnaire.questions.map(q => (
            <div key={q.number} style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 16, marginBottom: 12 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
                <strong>Q{q.number}. {q.text}</strong>
                <div>
                  <span style={{ background: '#dbeafe', color: '#1d4ed8', padding: '2px 8px', borderRadius: 12, fontSize: 12, marginRight: 4 }}>{q.category}</span>
                  <span style={{ background: q.difficulty === 'Hard' ? '#fef2f2' : q.difficulty === 'Medium' ? '#fefce8' : '#f0fdf4', color: q.difficulty === 'Hard' ? '#dc2626' : q.difficulty === 'Medium' ? '#ca8a04' : '#16a34a', padding: '2px 8px', borderRadius: 12, fontSize: 12 }}>{q.difficulty}</span>
                </div>
              </div>
              <textarea
                placeholder="Enter candidate's answer..."
                rows={3}
                value={answers[q.number] || ''}
                onChange={e => setAnswers({ ...answers, [q.number]: e.target.value })}
                style={{ width: '100%', padding: 8, border: '1px solid #d1d5db', borderRadius: 8, boxSizing: 'border-box' }}
              />
            </div>
          ))}
          <button onClick={handleSubmitAnswers} disabled={loading}
            style={{ background: '#3b82f6', color: '#fff', padding: '10px 24px', border: 'none', borderRadius: 8, cursor: 'pointer', opacity: loading ? 0.5 : 1 }}>
            {loading ? 'Evaluating Answers...' : 'Submit & Evaluate Answers'}
          </button>
        </div>
      )}

      {activeTab === 'questionnaire' && !questionnaire && (
        <p style={{ color: '#9ca3af' }}>No questionnaire generated yet. Evaluate a candidate first and generate one from the Evaluate tab.</p>
      )}

      {/* L1 Feedback Tab */}
      {activeTab === 'l1feedback' && l1Feedback && (
        <div>
          <h2 style={{ fontSize: 20, marginBottom: 16 }}>L1 Feedback - {l1Feedback.candidateName}</h2>

          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16, background: l1Feedback.recommendedForTechRound ? '#f0fdf4' : '#fef2f2', padding: 16, borderRadius: 8 }}>
            <div>
              <h3 style={{ margin: 0 }}>Overall L1 Score</h3>
              <p style={{ margin: 0, color: '#6b7280' }}>{l1Feedback.recommendedForTechRound ? 'Recommended for Tech Hands-On Round' : 'Not Recommended for Tech Round'}</p>
            </div>
            <span style={{ fontSize: 32, fontWeight: 'bold', color: l1Feedback.overallL1Score >= 7 ? '#22c55e' : '#ef4444' }}>
              {l1Feedback.overallL1Score}/10
            </span>
          </div>

          <h3>Answer Evaluations</h3>
          {l1Feedback.answerEvaluations.map(ae => (
            <div key={ae.questionNumber} style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 12, marginBottom: 8 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                <strong>Q{ae.questionNumber}. {ae.question}</strong>
                <span style={{ color: ae.score >= 7 ? '#22c55e' : ae.score >= 4 ? '#f59e0b' : '#ef4444', fontWeight: 'bold' }}>{ae.score}/10</span>
              </div>
              <p style={{ color: '#374151', background: '#f9fafb', padding: 8, borderRadius: 4, margin: '8px 0' }}><em>Answer: {ae.candidateAnswer}</em></p>
              <p style={{ color: '#6b7280' }}>{ae.feedback}</p>
            </div>
          ))}

          <div style={{ background: '#f9fafb', borderRadius: 8, padding: 16, marginTop: 16 }}>
            <h4>Overall L1 Assessment</h4>
            <p>{l1Feedback.overallL1Feedback}</p>
          </div>

          <div style={{ background: '#eff6ff', borderRadius: 8, padding: 16, marginTop: 12 }}>
            <h4>Tech Hands-On Round Recommendation</h4>
            <p>{l1Feedback.techHandsOnRecommendation}</p>
            <h5>Areas to Probe:</h5>
            <ul>
              {l1Feedback.areasToProbeInTechRound.map((a, i) => <li key={i}>{a}</li>)}
            </ul>
          </div>

          {l1Feedback.recommendedForTechRound && currentEvaluation && (
            <button onClick={() => handleGenerateL2(currentEvaluation.id)} disabled={loading}
              style={{ marginTop: 16, background: '#8b5cf6', color: '#fff', padding: '10px 24px', border: 'none', borderRadius: 8, cursor: 'pointer', opacity: loading ? 0.5 : 1 }}>
              {loading ? 'Generating...' : 'Generate L2 Tech Round Challenges'}
            </button>
          )}
        </div>
      )}

      {activeTab === 'l1feedback' && !l1Feedback && (
        <p style={{ color: '#9ca3af' }}>No L1 feedback available yet. Generate a questionnaire and submit answers first.</p>
      )}

      {/* L2 Round Tab */}
      {activeTab === 'l2round' && l2Questionnaire && (
        <div>
          <h2 style={{ fontSize: 20, marginBottom: 16 }}>L2 Tech Round - {l2Questionnaire.candidateName}</h2>

          {[
            { key: 'systemDesign' as const, label: 'System Design', icon: '🏗️', q: l2Questionnaire.systemDesign },
            { key: 'handsOnCoding' as const, label: 'Hands-On Coding', icon: '💻', q: l2Questionnaire.handsOnCoding },
            { key: 'designThinking' as const, label: 'Design Thinking', icon: '🎨', q: l2Questionnaire.designThinking },
            { key: 'tradeOffAnalysis' as const, label: 'Trade-Off Analysis', icon: '⚖️', q: l2Questionnaire.tradeOffAnalysis },
          ].map(({ key, label, icon, q }) => (
            <div key={key} style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 20, marginBottom: 16 }}>
              <h3 style={{ fontSize: 16, marginBottom: 12 }}>{icon} {label}</h3>

              <div style={{ background: '#f9fafb', borderRadius: 8, padding: 16, marginBottom: 12 }}>
                <h4 style={{ marginTop: 0 }}>Scenario</h4>
                <p style={{ whiteSpace: 'pre-wrap' }}>{q.scenario}</p>
              </div>

              <div style={{ marginBottom: 8 }}>
                <strong>Evaluation Criteria:</strong>
                <ul style={{ margin: '4px 0' }}>
                  {q.evaluationCriteria.map((c, i) => <li key={i} style={{ color: '#6b7280', fontSize: 14 }}>{c}</li>)}
                </ul>
              </div>

              <textarea
                placeholder={`Enter candidate's ${label.toLowerCase()} response...`}
                rows={6}
                value={l2Answers[key]}
                onChange={e => setL2Answers({ ...l2Answers, [key]: e.target.value })}
                style={{ width: '100%', padding: 10, border: '1px solid #d1d5db', borderRadius: 8, boxSizing: 'border-box', fontFamily: 'monospace' }}
              />
            </div>
          ))}

          <button onClick={handleSubmitL2} disabled={loading}
            style={{ background: '#3b82f6', color: '#fff', padding: '10px 24px', border: 'none', borderRadius: 8, cursor: 'pointer', opacity: loading ? 0.5 : 1 }}>
            {loading ? 'Evaluating L2...' : 'Submit & Evaluate L2 Answers'}
          </button>
        </div>
      )}

      {activeTab === 'l2round' && !l2Questionnaire && (
        <p style={{ color: '#9ca3af' }}>No L2 challenges generated yet. Complete L1 round first and generate L2 from the L1 Feedback tab.</p>
      )}

      {/* L2 Feedback Tab */}
      {activeTab === 'l2feedback' && l2Assessment && (
        <div>
          <h2 style={{ fontSize: 20, marginBottom: 16 }}>L2 Result - {l2Assessment.candidateName}</h2>

          <div style={{
            display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16, padding: 16, borderRadius: 8,
            background: l2Assessment.recommendedForHire ? '#f0fdf4' : '#fef2f2'
          }}>
            <div>
              <h3 style={{ margin: 0 }}>Overall L2 Score</h3>
              <p style={{ margin: 0, fontWeight: 'bold', fontSize: 18,
                color: l2Assessment.hiringRecommendation.includes('No') ? '#ef4444' : '#22c55e'
              }}>
                {l2Assessment.hiringRecommendation}
              </p>
            </div>
            <span style={{ fontSize: 32, fontWeight: 'bold', color: l2Assessment.overallL2Score >= 7 ? '#22c55e' : l2Assessment.overallL2Score >= 4 ? '#f59e0b' : '#ef4444' }}>
              {l2Assessment.overallL2Score}/10
            </span>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginBottom: 20 }}>
            {[
              { label: 'System Design', data: l2Assessment.systemDesign },
              { label: 'Hands-On Coding', data: l2Assessment.handsOnCoding },
              { label: 'Design Thinking', data: l2Assessment.designThinking },
              { label: 'Trade-Off Analysis', data: l2Assessment.tradeOffAnalysis },
            ].map(({ label, data }) => (
              <div key={label} style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 16 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
                  <strong>{label}</strong>
                  <span style={{ fontWeight: 'bold', color: scoreColor(data.score) }}>{data.score}/10</span>
                </div>
                <div style={{ background: '#e5e7eb', borderRadius: 8, height: 8, marginBottom: 8 }}>
                  <div style={{ width: `${data.score * 10}%`, background: scoreColor(data.score), borderRadius: 8, height: 8 }} />
                </div>
                <p style={{ color: '#6b7280', fontSize: 14 }}>{data.feedback}</p>
              </div>
            ))}
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginBottom: 16 }}>
            <div style={{ background: '#f0fdf4', borderRadius: 8, padding: 16 }}>
              <h4 style={{ color: '#16a34a', marginTop: 0 }}>Strengths</h4>
              <p>{l2Assessment.strengths}</p>
            </div>
            <div style={{ background: '#fef2f2', borderRadius: 8, padding: 16 }}>
              <h4 style={{ color: '#dc2626', marginTop: 0 }}>Weaknesses</h4>
              <p>{l2Assessment.weaknesses}</p>
            </div>
          </div>

          <div style={{ background: '#f9fafb', borderRadius: 8, padding: 16 }}>
            <h4>Overall L2 Assessment</h4>
            <p>{l2Assessment.overallL2Feedback}</p>
          </div>
        </div>
      )}

      {activeTab === 'l2feedback' && !l2Assessment && (
        <p style={{ color: '#9ca3af' }}>No L2 result available yet. Complete L2 tech round first.</p>
      )}

      {loading && (
        <div style={{ position: 'fixed', bottom: 24, right: 24, background: '#3b82f6', color: '#fff', padding: '12px 20px', borderRadius: 8, boxShadow: '0 4px 12px rgba(0,0,0,0.15)' }}>
          Processing...
        </div>
      )}
    </div>
  );
};

export default App;
