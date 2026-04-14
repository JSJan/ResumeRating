import React, { useState } from 'react';
import {
  Resume, JobDescription, CandidateEvaluation, Questionnaire, L1Feedback,
  uploadResume, getResumes, createJobDescription, getJobDescriptions,
  evaluateResume, getEvaluations, generateQuestionnaire, submitL1Answers,
  seedFromAssets
} from './api';

type Tab = 'upload' | 'jobDescription' | 'evaluate' | 'questionnaire' | 'l1feedback';

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

  // JD state
  const [jdForm, setJdForm] = useState({ title: '', description: '', requiredSkills: '', preferredSkills: '', experienceLevel: '' });

  // Evaluate state
  const [selectedResume, setSelectedResume] = useState('');
  const [selectedJd, setSelectedJd] = useState('');
  const [currentEvaluation, setCurrentEvaluation] = useState<CandidateEvaluation | null>(null);

  // L1 answers state
  const [answers, setAnswers] = useState<Record<number, string>>({});

  const handleError = (err: unknown) => {
    if (err instanceof Error) setError(err.message);
    else setError('An error occurred');
  };

  const handleUpload = async () => {
    if (!selectedFile) return;
    setLoading(true);
    setError('');
    try {
      await uploadResume(selectedFile);
      const res = await getResumes();
      setResumes(res.data);
      setSelectedFile(null);
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
      <div style={{ display: 'flex', gap: 4, marginBottom: 24, borderBottom: '2px solid #e5e7eb' }}>
        {([['upload', 'Upload Resumes'], ['jobDescription', 'Job Description'], ['evaluate', 'Evaluate'], ['questionnaire', 'L1 Questionnaire'], ['l1feedback', 'L1 Feedback']] as [Tab, string][]).map(([key, label]) => (
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
                    <th style={{ padding: 8, textAlign: 'left', borderBottom: '1px solid #e5e7eb' }}>Uploaded</th>
                  </tr>
                </thead>
                <tbody>
                  {resumes.map(r => (
                    <tr key={r.id}>
                      <td style={{ padding: 8, borderBottom: '1px solid #e5e7eb' }}>{r.candidateName}</td>
                      <td style={{ padding: 8, borderBottom: '1px solid #e5e7eb' }}>{r.fileName}</td>
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
        </div>
      )}

      {activeTab === 'l1feedback' && !l1Feedback && (
        <p style={{ color: '#9ca3af' }}>No L1 feedback available yet. Generate a questionnaire and submit answers first.</p>
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
