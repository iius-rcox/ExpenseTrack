import { useState, useMemo } from 'react';
import {
  MappingOption,
  ColumnFieldType,
  COLUMN_FIELD_OPTIONS,
} from '../../services/statementService';

export interface ColumnMappingEditorProps {
  headers: string[];
  sampleRows: string[][];
  mappingOptions: MappingOption[];
  onConfirm: (mapping: Record<string, ColumnFieldType>, amountSign: 'negative_charges' | 'positive_charges', fingerprintName?: string) => void;
  onCancel: () => void;
}

export function ColumnMappingEditor({
  headers,
  sampleRows,
  mappingOptions,
  onConfirm,
  onCancel,
}: ColumnMappingEditorProps) {
  // Select the first mapping option by default
  const [selectedOptionIndex, setSelectedOptionIndex] = useState(0);
  const selectedOption = mappingOptions[selectedOptionIndex];

  // Initialize editable mapping from selected option
  const [columnMapping, setColumnMapping] = useState<Record<string, ColumnFieldType>>(
    () => ({ ...selectedOption.columnMapping })
  );
  const [amountSign, setAmountSign] = useState<'negative_charges' | 'positive_charges'>(
    selectedOption.amountSign
  );
  const [saveFingerprint, setSaveFingerprint] = useState(
    selectedOption.source === 'ai_inference'
  );
  const [fingerprintName, setFingerprintName] = useState('');

  // Track if user has modified the mapping
  const [isModified, setIsModified] = useState(false);

  // When selected option changes, update the mapping
  const handleOptionChange = (index: number) => {
    setSelectedOptionIndex(index);
    const option = mappingOptions[index];
    setColumnMapping({ ...option.columnMapping });
    setAmountSign(option.amountSign);
    setIsModified(false);
    // Auto-enable save for AI-inferred mappings
    setSaveFingerprint(option.source === 'ai_inference');
  };

  // Update individual column mapping
  const handleColumnChange = (header: string, fieldType: ColumnFieldType) => {
    setColumnMapping((prev) => ({
      ...prev,
      [header]: fieldType,
    }));
    setIsModified(true);
  };

  // Validation: check required fields are mapped
  const validation = useMemo(() => {
    const mappedFields = new Set(Object.values(columnMapping));
    const missingRequired = COLUMN_FIELD_OPTIONS
      .filter((f) => f.required && !mappedFields.has(f.value))
      .map((f) => f.label);

    // Check for duplicate non-ignore mappings
    const nonIgnoreMappings = Object.entries(columnMapping)
      .filter(([, type]) => type !== 'ignore');
    const duplicates = nonIgnoreMappings
      .filter(([, type], i, arr) =>
        arr.findIndex(([, t]) => t === type) !== i && type !== 'ignore'
      )
      .map(([header]) => header);

    return {
      isValid: missingRequired.length === 0 && duplicates.length === 0,
      missingRequired,
      duplicates,
    };
  }, [columnMapping]);

  const handleConfirm = () => {
    if (!validation.isValid) return;
    onConfirm(
      columnMapping,
      amountSign,
      saveFingerprint ? fingerprintName || undefined : undefined
    );
  };

  const getTierLabel = (option: MappingOption) => {
    switch (option.source) {
      case 'system_fingerprint':
        return 'System';
      case 'user_fingerprint':
        return 'Saved';
      case 'ai_inference':
        return 'AI';
    }
  };

  const getTierColor = (option: MappingOption) => {
    switch (option.source) {
      case 'system_fingerprint':
        return '#059669'; // green
      case 'user_fingerprint':
        return '#2563eb'; // blue
      case 'ai_inference':
        return '#d97706'; // amber
    }
  };

  return (
    <div className="column-mapping-editor">
      {/* Mapping source selector */}
      {mappingOptions.length > 1 && (
        <div className="mapping-options">
          <h3>Choose Mapping Source</h3>
          <div className="options-list">
            {mappingOptions.map((option, index) => (
              <button
                key={index}
                className={`option-button ${selectedOptionIndex === index ? 'selected' : ''}`}
                onClick={() => handleOptionChange(index)}
              >
                <span
                  className="tier-badge"
                  style={{ backgroundColor: getTierColor(option) }}
                >
                  {getTierLabel(option)}
                </span>
                <span className="option-name">
                  {option.sourceName || 'AI Detected'}
                </span>
                {option.confidence !== undefined && (
                  <span className="confidence">
                    {Math.round(option.confidence * 100)}% confidence
                  </span>
                )}
              </button>
            ))}
          </div>
        </div>
      )}

      {/* Column mapping table */}
      <div className="mapping-table-container">
        <h3>Column Mapping {isModified && <span className="modified-badge">Modified</span>}</h3>
        <table className="mapping-table">
          <thead>
            <tr>
              <th>Column Header</th>
              <th>Maps To</th>
              {sampleRows.length > 0 && <th>Sample Values</th>}
            </tr>
          </thead>
          <tbody>
            {headers.map((header, colIndex) => (
              <tr key={header}>
                <td className="header-cell">{header}</td>
                <td>
                  <select
                    value={columnMapping[header] || 'ignore'}
                    onChange={(e) =>
                      handleColumnChange(header, e.target.value as ColumnFieldType)
                    }
                    className={`field-select ${
                      COLUMN_FIELD_OPTIONS.find((f) => f.value === columnMapping[header])?.required
                        ? 'required-field'
                        : ''
                    }`}
                  >
                    {COLUMN_FIELD_OPTIONS.map((field) => (
                      <option key={field.value} value={field.value}>
                        {field.label}
                        {field.required ? ' *' : ''}
                      </option>
                    ))}
                  </select>
                </td>
                {sampleRows.length > 0 && (
                  <td className="sample-cell">
                    {sampleRows.slice(0, 3).map((row, rowIndex) => (
                      <span key={rowIndex} className="sample-value">
                        {row[colIndex] || '(empty)'}
                      </span>
                    ))}
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Amount sign convention */}
      <div className="amount-sign-section">
        <h3>Amount Convention</h3>
        <div className="radio-group">
          <label className="radio-label">
            <input
              type="radio"
              name="amountSign"
              value="negative_charges"
              checked={amountSign === 'negative_charges'}
              onChange={() => {
                setAmountSign('negative_charges');
                setIsModified(true);
              }}
            />
            <span>Negative amounts = charges (Chase, most banks)</span>
          </label>
          <label className="radio-label">
            <input
              type="radio"
              name="amountSign"
              value="positive_charges"
              checked={amountSign === 'positive_charges'}
              onChange={() => {
                setAmountSign('positive_charges');
                setIsModified(true);
              }}
            />
            <span>Positive amounts = charges (American Express)</span>
          </label>
        </div>
      </div>

      {/* Save as fingerprint option */}
      <div className="save-fingerprint-section">
        <label className="checkbox-label">
          <input
            type="checkbox"
            checked={saveFingerprint}
            onChange={(e) => setSaveFingerprint(e.target.checked)}
          />
          <span>Save this mapping for future imports</span>
        </label>
        {saveFingerprint && (
          <input
            type="text"
            placeholder="Name for saved mapping (optional)"
            value={fingerprintName}
            onChange={(e) => setFingerprintName(e.target.value)}
            className="fingerprint-name-input"
          />
        )}
      </div>

      {/* Validation messages */}
      {!validation.isValid && (
        <div className="validation-errors">
          {validation.missingRequired.length > 0 && (
            <p className="error">
              Missing required fields: {validation.missingRequired.join(', ')}
            </p>
          )}
          {validation.duplicates.length > 0 && (
            <p className="error">
              Duplicate mappings for columns: {validation.duplicates.join(', ')}
            </p>
          )}
        </div>
      )}

      {/* Actions */}
      <div className="actions">
        <button className="cancel-button" onClick={onCancel}>
          Cancel
        </button>
        <button
          className="confirm-button"
          onClick={handleConfirm}
          disabled={!validation.isValid}
        >
          Import Transactions
        </button>
      </div>

      <style>{`
        .column-mapping-editor {
          padding: 24px;
          max-width: 900px;
          margin: 0 auto;
        }

        h3 {
          font-size: 16px;
          font-weight: 600;
          color: #111827;
          margin: 0 0 12px;
        }

        .mapping-options {
          margin-bottom: 24px;
        }

        .options-list {
          display: flex;
          gap: 12px;
          flex-wrap: wrap;
        }

        .option-button {
          display: flex;
          align-items: center;
          gap: 8px;
          padding: 12px 16px;
          border: 2px solid #e5e7eb;
          border-radius: 8px;
          background: white;
          cursor: pointer;
          transition: all 0.2s;
        }

        .option-button:hover {
          border-color: #6366f1;
        }

        .option-button.selected {
          border-color: #6366f1;
          background: #f5f3ff;
        }

        .tier-badge {
          padding: 2px 8px;
          border-radius: 4px;
          color: white;
          font-size: 12px;
          font-weight: 500;
        }

        .option-name {
          font-weight: 500;
          color: #374151;
        }

        .confidence {
          font-size: 12px;
          color: #6b7280;
        }

        .mapping-table-container {
          margin-bottom: 24px;
          overflow-x: auto;
        }

        .modified-badge {
          font-size: 12px;
          font-weight: normal;
          color: #d97706;
          margin-left: 8px;
        }

        .mapping-table {
          width: 100%;
          border-collapse: collapse;
          font-size: 14px;
        }

        .mapping-table th,
        .mapping-table td {
          padding: 12px;
          text-align: left;
          border-bottom: 1px solid #e5e7eb;
        }

        .mapping-table th {
          background: #f9fafb;
          font-weight: 500;
          color: #374151;
        }

        .header-cell {
          font-family: monospace;
          font-size: 13px;
          color: #111827;
        }

        .field-select {
          padding: 8px 12px;
          border: 1px solid #d1d5db;
          border-radius: 6px;
          font-size: 14px;
          min-width: 160px;
          background: white;
        }

        .field-select:focus {
          outline: none;
          border-color: #6366f1;
          box-shadow: 0 0 0 2px rgba(99, 102, 241, 0.2);
        }

        .field-select.required-field {
          border-color: #6366f1;
        }

        .sample-cell {
          max-width: 300px;
        }

        .sample-value {
          display: block;
          font-size: 12px;
          color: #6b7280;
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
          max-width: 200px;
        }

        .sample-value + .sample-value {
          margin-top: 4px;
        }

        .amount-sign-section {
          margin-bottom: 24px;
        }

        .radio-group {
          display: flex;
          flex-direction: column;
          gap: 8px;
        }

        .radio-label {
          display: flex;
          align-items: center;
          gap: 8px;
          cursor: pointer;
          font-size: 14px;
          color: #374151;
        }

        .radio-label input {
          width: 16px;
          height: 16px;
        }

        .save-fingerprint-section {
          margin-bottom: 24px;
          padding: 16px;
          background: #f9fafb;
          border-radius: 8px;
        }

        .checkbox-label {
          display: flex;
          align-items: center;
          gap: 8px;
          cursor: pointer;
          font-size: 14px;
          color: #374151;
        }

        .checkbox-label input {
          width: 16px;
          height: 16px;
        }

        .fingerprint-name-input {
          margin-top: 12px;
          padding: 8px 12px;
          border: 1px solid #d1d5db;
          border-radius: 6px;
          font-size: 14px;
          width: 100%;
          max-width: 300px;
        }

        .fingerprint-name-input:focus {
          outline: none;
          border-color: #6366f1;
          box-shadow: 0 0 0 2px rgba(99, 102, 241, 0.2);
        }

        .validation-errors {
          margin-bottom: 16px;
          padding: 12px 16px;
          background: #fef2f2;
          border: 1px solid #fecaca;
          border-radius: 8px;
        }

        .validation-errors .error {
          margin: 0;
          color: #dc2626;
          font-size: 14px;
        }

        .validation-errors .error + .error {
          margin-top: 8px;
        }

        .actions {
          display: flex;
          justify-content: flex-end;
          gap: 12px;
          padding-top: 16px;
          border-top: 1px solid #e5e7eb;
        }

        .cancel-button {
          padding: 10px 20px;
          border: 1px solid #d1d5db;
          border-radius: 6px;
          background: white;
          color: #374151;
          font-size: 14px;
          font-weight: 500;
          cursor: pointer;
          transition: all 0.2s;
        }

        .cancel-button:hover {
          background: #f9fafb;
        }

        .confirm-button {
          padding: 10px 20px;
          border: none;
          border-radius: 6px;
          background: #6366f1;
          color: white;
          font-size: 14px;
          font-weight: 500;
          cursor: pointer;
          transition: all 0.2s;
        }

        .confirm-button:hover:not(:disabled) {
          background: #4f46e5;
        }

        .confirm-button:disabled {
          background: #a5b4fc;
          cursor: not-allowed;
        }
      `}</style>
    </div>
  );
}

export default ColumnMappingEditor;
