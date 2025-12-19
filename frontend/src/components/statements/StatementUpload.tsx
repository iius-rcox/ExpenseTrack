import React, { useCallback, useState } from 'react';
import { useMsal } from '@azure/msal-react';
import { InteractionRequiredAuthError } from '@azure/msal-browser';
import { apiScopes, loginRequest } from '../../auth/authConfig';
import { analyzeStatement, StatementAnalyzeResponse, ApiError } from '../../services/statementService';

export interface StatementUploadProps {
  onAnalysisComplete: (response: StatementAnalyzeResponse) => void;
  onError: (error: string) => void;
}

const ACCEPTED_FILE_TYPES = '.csv,.xlsx,.xls';
const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB

export function StatementUpload({ onAnalysisComplete, onError }: StatementUploadProps) {
  const { instance, accounts } = useMsal();
  const [isDragging, setIsDragging] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  const validateFile = (file: File): string | null => {
    const extension = file.name.split('.').pop()?.toLowerCase();
    if (!extension || !['csv', 'xlsx', 'xls'].includes(extension)) {
      return 'Please upload a CSV or Excel file (.csv, .xlsx, .xls)';
    }
    if (file.size > MAX_FILE_SIZE) {
      return 'File size must be less than 10MB';
    }
    return null;
  };

  const getToken = async (): Promise<string | null> => {
    if (accounts.length === 0) {
      return null;
    }
    try {
      const response = await instance.acquireTokenSilent({
        scopes: apiScopes.all,
        account: accounts[0],
      });
      return response.accessToken;
    } catch (error) {
      if (error instanceof InteractionRequiredAuthError) {
        // Token expired - redirect to login
        await instance.loginRedirect(loginRequest);
        return null;
      }
      console.error('Failed to acquire token:', error);
      return null;
    }
  };

  const handleFileSelect = useCallback(async (file: File) => {
    const validationError = validateFile(file);
    if (validationError) {
      onError(validationError);
      return;
    }

    setSelectedFile(file);
    setIsUploading(true);

    try {
      const token = await getToken();
      if (!token) {
        onError('Authentication required. Please sign in again.');
        return;
      }
      const response = await analyzeStatement(file, token);
      onAnalysisComplete(response);
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 401) {
          onError('Session expired. Please sign in again.');
        } else if (err.status === 503) {
          onError('AI service is currently unavailable. Please try again later or upload a known statement format.');
        } else {
          onError(err.detail || err.title);
        }
      } else {
        onError('Failed to analyze file. Please try again.');
      }
    } finally {
      setIsUploading(false);
    }
  }, [onAnalysisComplete, onError, accounts, instance]);

  const handleDrop = useCallback((e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(false);

    const files = e.dataTransfer.files;
    if (files.length > 0) {
      handleFileSelect(files[0]);
    }
  }, [handleFileSelect]);

  const handleDragOver = useCallback((e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(true);
  }, []);

  const handleDragLeave = useCallback((e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(false);
  }, []);

  const handleInputChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (files && files.length > 0) {
      handleFileSelect(files[0]);
    }
  }, [handleFileSelect]);

  return (
    <div className="statement-upload">
      <div
        className={`upload-zone ${isDragging ? 'dragging' : ''} ${isUploading ? 'uploading' : ''}`}
        onDrop={handleDrop}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
      >
        {isUploading ? (
          <div className="upload-progress">
            <div className="spinner" aria-label="Analyzing file..." />
            <p>Analyzing {selectedFile?.name}...</p>
            <p className="hint">Detecting column mappings</p>
          </div>
        ) : (
          <>
            <div className="upload-icon">
              <svg
                xmlns="http://www.w3.org/2000/svg"
                width="48"
                height="48"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
                <polyline points="17 8 12 3 7 8" />
                <line x1="12" y1="3" x2="12" y2="15" />
              </svg>
            </div>
            <p className="upload-text">
              Drag & drop your statement file here, or{' '}
              <label className="browse-link">
                browse
                <input
                  type="file"
                  accept={ACCEPTED_FILE_TYPES}
                  onChange={handleInputChange}
                  hidden
                />
              </label>
            </p>
            <p className="upload-hint">Supports CSV, Excel (.xlsx, .xls)</p>
          </>
        )}
      </div>

      <style>{`
        .statement-upload {
          width: 100%;
        }

        .upload-zone {
          border: 2px dashed #d1d5db;
          border-radius: 8px;
          padding: 48px 24px;
          text-align: center;
          transition: all 0.2s ease;
          cursor: pointer;
          background: #f9fafb;
        }

        .upload-zone:hover {
          border-color: #6366f1;
          background: #f5f3ff;
        }

        .upload-zone.dragging {
          border-color: #6366f1;
          background: #eef2ff;
          border-style: solid;
        }

        .upload-zone.uploading {
          cursor: default;
          background: #f9fafb;
        }

        .upload-icon {
          color: #9ca3af;
          margin-bottom: 16px;
        }

        .upload-zone.dragging .upload-icon {
          color: #6366f1;
        }

        .upload-text {
          font-size: 16px;
          color: #374151;
          margin: 0 0 8px;
        }

        .browse-link {
          color: #6366f1;
          cursor: pointer;
          text-decoration: underline;
        }

        .browse-link:hover {
          color: #4f46e5;
        }

        .upload-hint {
          font-size: 14px;
          color: #6b7280;
          margin: 0;
        }

        .upload-progress {
          padding: 24px 0;
        }

        .upload-progress p {
          margin: 8px 0 0;
          color: #374151;
        }

        .upload-progress .hint {
          font-size: 14px;
          color: #6b7280;
        }

        .spinner {
          width: 40px;
          height: 40px;
          border: 3px solid #e5e7eb;
          border-top-color: #6366f1;
          border-radius: 50%;
          animation: spin 1s linear infinite;
          margin: 0 auto 16px;
        }

        @keyframes spin {
          to {
            transform: rotate(360deg);
          }
        }
      `}</style>
    </div>
  );
}

export default StatementUpload;
