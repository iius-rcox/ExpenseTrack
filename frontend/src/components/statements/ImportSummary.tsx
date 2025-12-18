import { StatementImportResponse, TransactionSummary } from '../../services/statementService';

export interface ImportSummaryProps {
  result: StatementImportResponse;
  onViewTransactions: () => void;
  onImportAnother: () => void;
}

export function ImportSummary({
  result,
  onViewTransactions,
  onImportAnother,
}: ImportSummaryProps) {
  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(Math.abs(amount));
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  };

  return (
    <div className="import-summary">
      <div className="success-header">
        <div className="success-icon">
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
            <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
            <polyline points="22 4 12 14.01 9 11.01" />
          </svg>
        </div>
        <h2>Import Complete</h2>
        <p className="tier-info">
          {result.tierUsed === 1
            ? 'Used cached fingerprint (Tier 1)'
            : 'Used AI detection (Tier 3)'}
        </p>
      </div>

      <div className="stats-grid">
        <div className="stat-card imported">
          <span className="stat-value">{result.imported}</span>
          <span className="stat-label">Imported</span>
        </div>
        <div className="stat-card skipped">
          <span className="stat-value">{result.skipped}</span>
          <span className="stat-label">Skipped</span>
          {result.skipped > 0 && (
            <span className="stat-hint">Missing required fields</span>
          )}
        </div>
        <div className="stat-card duplicates">
          <span className="stat-value">{result.duplicates}</span>
          <span className="stat-label">Duplicates</span>
          {result.duplicates > 0 && (
            <span className="stat-hint">Already in system</span>
          )}
        </div>
      </div>

      {result.fingerprintSaved && (
        <div className="fingerprint-saved-notice">
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
            <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z" />
            <polyline points="17 21 17 13 7 13 7 21" />
            <polyline points="7 3 7 8 15 8" />
          </svg>
          <span>Mapping saved for future imports</span>
        </div>
      )}

      {result.transactions.length > 0 && (
        <div className="preview-section">
          <h3>Imported Transactions Preview</h3>
          <table className="preview-table">
            <thead>
              <tr>
                <th>Date</th>
                <th>Description</th>
                <th className="amount-col">Amount</th>
              </tr>
            </thead>
            <tbody>
              {result.transactions.slice(0, 10).map((tx: TransactionSummary) => (
                <tr key={tx.id}>
                  <td>{formatDate(tx.transactionDate)}</td>
                  <td className="description-cell">{tx.description}</td>
                  <td className={`amount-cell ${tx.amount < 0 ? 'expense' : 'credit'}`}>
                    {tx.amount < 0 ? '-' : '+'}
                    {formatCurrency(tx.amount)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {result.imported > 10 && (
            <p className="more-transactions">
              and {result.imported - 10} more transactions...
            </p>
          )}
        </div>
      )}

      <div className="actions">
        <button className="secondary-button" onClick={onImportAnother}>
          Import Another Statement
        </button>
        <button className="primary-button" onClick={onViewTransactions}>
          View All Transactions
        </button>
      </div>

      <style>{`
        .import-summary {
          padding: 24px;
          max-width: 700px;
          margin: 0 auto;
        }

        .success-header {
          text-align: center;
          margin-bottom: 32px;
        }

        .success-icon {
          color: #059669;
          margin-bottom: 16px;
        }

        .success-header h2 {
          font-size: 24px;
          font-weight: 600;
          color: #111827;
          margin: 0 0 8px;
        }

        .tier-info {
          font-size: 14px;
          color: #6b7280;
          margin: 0;
        }

        .stats-grid {
          display: grid;
          grid-template-columns: repeat(3, 1fr);
          gap: 16px;
          margin-bottom: 24px;
        }

        .stat-card {
          padding: 20px;
          border-radius: 8px;
          text-align: center;
          display: flex;
          flex-direction: column;
          gap: 4px;
        }

        .stat-card.imported {
          background: #ecfdf5;
          border: 1px solid #a7f3d0;
        }

        .stat-card.skipped {
          background: #fffbeb;
          border: 1px solid #fde68a;
        }

        .stat-card.duplicates {
          background: #f3f4f6;
          border: 1px solid #e5e7eb;
        }

        .stat-value {
          font-size: 32px;
          font-weight: 700;
          line-height: 1;
        }

        .stat-card.imported .stat-value {
          color: #059669;
        }

        .stat-card.skipped .stat-value {
          color: #d97706;
        }

        .stat-card.duplicates .stat-value {
          color: #6b7280;
        }

        .stat-label {
          font-size: 14px;
          font-weight: 500;
          color: #374151;
        }

        .stat-hint {
          font-size: 12px;
          color: #6b7280;
        }

        .fingerprint-saved-notice {
          display: flex;
          align-items: center;
          gap: 8px;
          padding: 12px 16px;
          background: #f0fdf4;
          border: 1px solid #bbf7d0;
          border-radius: 8px;
          margin-bottom: 24px;
          color: #166534;
          font-size: 14px;
        }

        .preview-section {
          margin-bottom: 24px;
        }

        .preview-section h3 {
          font-size: 16px;
          font-weight: 600;
          color: #111827;
          margin: 0 0 12px;
        }

        .preview-table {
          width: 100%;
          border-collapse: collapse;
          font-size: 14px;
        }

        .preview-table th,
        .preview-table td {
          padding: 10px 12px;
          text-align: left;
          border-bottom: 1px solid #e5e7eb;
        }

        .preview-table th {
          background: #f9fafb;
          font-weight: 500;
          color: #374151;
        }

        .amount-col {
          text-align: right;
        }

        .description-cell {
          max-width: 300px;
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
        }

        .amount-cell {
          text-align: right;
          font-family: 'SF Mono', 'Monaco', monospace;
          font-size: 13px;
        }

        .amount-cell.expense {
          color: #dc2626;
        }

        .amount-cell.credit {
          color: #059669;
        }

        .more-transactions {
          text-align: center;
          font-size: 14px;
          color: #6b7280;
          margin: 12px 0 0;
        }

        .actions {
          display: flex;
          justify-content: center;
          gap: 12px;
          padding-top: 16px;
          border-top: 1px solid #e5e7eb;
        }

        .secondary-button {
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

        .secondary-button:hover {
          background: #f9fafb;
        }

        .primary-button {
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

        .primary-button:hover {
          background: #4f46e5;
        }

        @media (max-width: 640px) {
          .stats-grid {
            grid-template-columns: 1fr;
          }

          .actions {
            flex-direction: column;
          }

          .actions button {
            width: 100%;
          }
        }
      `}</style>
    </div>
  );
}

export default ImportSummary;
