import React, { useState, useCallback } from 'react';
import { StatementUpload } from '../components/statements/StatementUpload';
import { ColumnMappingEditor } from '../components/statements/ColumnMappingEditor';
import { ImportSummary } from '../components/statements/ImportSummary';
import {
  StatementAnalyzeResponse,
  StatementImportResponse,
  importStatement,
  ColumnFieldType,
  ApiError,
} from '../services/statementService';

type WizardStep = 'upload' | 'mapping' | 'importing' | 'complete';

export function StatementImportPage() {
  const [step, setStep] = useState<WizardStep>('upload');
  const [error, setError] = useState<string | null>(null);
  const [analysisResult, setAnalysisResult] = useState<StatementAnalyzeResponse | null>(null);
  const [importResult, setImportResult] = useState<StatementImportResponse | null>(null);

  const handleAnalysisComplete = useCallback((response: StatementAnalyzeResponse) => {
    setAnalysisResult(response);
    setError(null);
    setStep('mapping');
  }, []);

  const handleError = useCallback((errorMessage: string) => {
    setError(errorMessage);
  }, []);

  const handleConfirmMapping = useCallback(
    async (
      mapping: Record<string, ColumnFieldType>,
      amountSign: 'negative_charges' | 'positive_charges',
      fingerprintName?: string
    ) => {
      if (!analysisResult) return;

      setStep('importing');
      setError(null);

      try {
        const result = await importStatement({
          analysisId: analysisResult.analysisId,
          columnMapping: mapping,
          amountSign,
          saveAsFingerprint: fingerprintName !== undefined || true,
          fingerprintName,
        });
        setImportResult(result);
        setStep('complete');
      } catch (err) {
        if (err instanceof ApiError) {
          if (err.status === 400 && err.detail?.includes('expired')) {
            setError('Analysis session expired. Please upload the file again.');
            setStep('upload');
          } else {
            setError(err.detail || err.title);
            setStep('mapping');
          }
        } else {
          setError('Import failed. Please try again.');
          setStep('mapping');
        }
      }
    },
    [analysisResult]
  );

  const handleCancel = useCallback(() => {
    setStep('upload');
    setAnalysisResult(null);
    setError(null);
  }, []);

  const handleImportAnother = useCallback(() => {
    setStep('upload');
    setAnalysisResult(null);
    setImportResult(null);
    setError(null);
  }, []);

  const handleViewTransactions = useCallback(() => {
    // Navigate to transactions page - implementation depends on router
    window.location.href = '/transactions';
  }, []);

  const getStepNumber = (currentStep: WizardStep): number => {
    switch (currentStep) {
      case 'upload':
        return 1;
      case 'mapping':
      case 'importing':
        return 2;
      case 'complete':
        return 3;
    }
  };

  return (
    <div className="statement-import-page">
      <header className="page-header">
        <h1>Import Statement</h1>
        <p className="page-description">
          Upload a CSV or Excel file from your bank or credit card provider
        </p>
      </header>

      {/* Progress indicator */}
      <div className="progress-steps">
        <div className={`step ${getStepNumber(step) >= 1 ? 'active' : ''} ${getStepNumber(step) > 1 ? 'complete' : ''}`}>
          <div className="step-number">1</div>
          <div className="step-label">Upload</div>
        </div>
        <div className="step-connector" />
        <div className={`step ${getStepNumber(step) >= 2 ? 'active' : ''} ${getStepNumber(step) > 2 ? 'complete' : ''}`}>
          <div className="step-number">2</div>
          <div className="step-label">Map Columns</div>
        </div>
        <div className="step-connector" />
        <div className={`step ${getStepNumber(step) >= 3 ? 'active' : ''}`}>
          <div className="step-number">3</div>
          <div className="step-label">Complete</div>
        </div>
      </div>

      {/* Error display */}
      {error && (
        <div className="error-banner" role="alert">
          <svg
            xmlns="http://www.w3.org/2000/svg"
            width="20"
            height="20"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <circle cx="12" cy="12" r="10" />
            <line x1="12" y1="8" x2="12" y2="12" />
            <line x1="12" y1="16" x2="12.01" y2="16" />
          </svg>
          <span>{error}</span>
          <button
            className="dismiss-button"
            onClick={() => setError(null)}
            aria-label="Dismiss error"
          >
            &times;
          </button>
        </div>
      )}

      {/* Step content */}
      <div className="step-content">
        {step === 'upload' && (
          <StatementUpload
            onAnalysisComplete={handleAnalysisComplete}
            onError={handleError}
          />
        )}

        {step === 'mapping' && analysisResult && (
          <ColumnMappingEditor
            headers={analysisResult.headers}
            sampleRows={analysisResult.sampleRows}
            mappingOptions={analysisResult.mappingOptions}
            onConfirm={handleConfirmMapping}
            onCancel={handleCancel}
          />
        )}

        {step === 'importing' && (
          <div className="importing-state">
            <div className="spinner" aria-label="Importing..." />
            <p>Importing transactions...</p>
            <p className="hint">This may take a moment for large files</p>
          </div>
        )}

        {step === 'complete' && importResult && (
          <ImportSummary
            result={importResult}
            onViewTransactions={handleViewTransactions}
            onImportAnother={handleImportAnother}
          />
        )}
      </div>

      <style>{`
        .statement-import-page {
          max-width: 960px;
          margin: 0 auto;
          padding: 32px 24px;
        }

        .page-header {
          text-align: center;
          margin-bottom: 32px;
        }

        .page-header h1 {
          font-size: 28px;
          font-weight: 700;
          color: #111827;
          margin: 0 0 8px;
        }

        .page-description {
          font-size: 16px;
          color: #6b7280;
          margin: 0;
        }

        .progress-steps {
          display: flex;
          align-items: center;
          justify-content: center;
          margin-bottom: 32px;
        }

        .step {
          display: flex;
          flex-direction: column;
          align-items: center;
          gap: 8px;
        }

        .step-number {
          width: 32px;
          height: 32px;
          border-radius: 50%;
          background: #e5e7eb;
          color: #9ca3af;
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: 14px;
          font-weight: 600;
          transition: all 0.2s;
        }

        .step.active .step-number {
          background: #6366f1;
          color: white;
        }

        .step.complete .step-number {
          background: #059669;
          color: white;
        }

        .step-label {
          font-size: 12px;
          font-weight: 500;
          color: #9ca3af;
        }

        .step.active .step-label {
          color: #374151;
        }

        .step-connector {
          width: 80px;
          height: 2px;
          background: #e5e7eb;
          margin: 0 16px 24px;
        }

        .error-banner {
          display: flex;
          align-items: center;
          gap: 12px;
          padding: 12px 16px;
          background: #fef2f2;
          border: 1px solid #fecaca;
          border-radius: 8px;
          margin-bottom: 24px;
          color: #dc2626;
        }

        .error-banner span {
          flex: 1;
          font-size: 14px;
        }

        .dismiss-button {
          background: none;
          border: none;
          color: #dc2626;
          font-size: 20px;
          cursor: pointer;
          padding: 0;
          line-height: 1;
        }

        .dismiss-button:hover {
          color: #b91c1c;
        }

        .step-content {
          background: white;
          border-radius: 12px;
          box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
          min-height: 300px;
        }

        .importing-state {
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          padding: 80px 24px;
        }

        .importing-state p {
          margin: 8px 0 0;
          color: #374151;
        }

        .importing-state .hint {
          font-size: 14px;
          color: #6b7280;
        }

        .spinner {
          width: 48px;
          height: 48px;
          border: 4px solid #e5e7eb;
          border-top-color: #6366f1;
          border-radius: 50%;
          animation: spin 1s linear infinite;
        }

        @keyframes spin {
          to {
            transform: rotate(360deg);
          }
        }

        @media (max-width: 640px) {
          .statement-import-page {
            padding: 16px;
          }

          .page-header h1 {
            font-size: 24px;
          }

          .step-connector {
            width: 40px;
            margin: 0 8px 24px;
          }

          .step-label {
            font-size: 10px;
          }
        }
      `}</style>
    </div>
  );
}

export default StatementImportPage;
