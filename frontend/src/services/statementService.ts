/**
 * Statement Import API Service
 *
 * Provides client-side API calls for the statement import & fingerprinting feature.
 * Matches the contracts defined in /specs/004-statement-fingerprinting/contracts/statements-api.yaml
 */

// Types matching API contract
export interface MappingOption {
  source: 'system_fingerprint' | 'user_fingerprint' | 'ai_inference';
  tier: 1 | 3;
  fingerprintId?: string;
  sourceName?: string;
  columnMapping: Record<string, ColumnFieldType>;
  dateFormat?: string;
  amountSign: 'negative_charges' | 'positive_charges';
  confidence?: number;
}

export type ColumnFieldType =
  | 'date'
  | 'post_date'
  | 'description'
  | 'amount'
  | 'category'
  | 'memo'
  | 'reference'
  | 'ignore';

export interface StatementAnalyzeResponse {
  analysisId: string;
  fileName: string;
  rowCount: number;
  headers: string[];
  sampleRows: string[][];
  mappingOptions: MappingOption[];
}

export interface StatementImportRequest {
  analysisId: string;
  columnMapping: Record<string, string>;
  dateFormat?: string;
  amountSign: 'negative_charges' | 'positive_charges';
  saveAsFingerprint?: boolean;
  fingerprintName?: string;
}

export interface TransactionSummary {
  id: string;
  transactionDate: string;
  description: string;
  amount: number;
  hasMatchedReceipt: boolean;
}

export interface StatementImportResponse {
  importId: string;
  tierUsed: 1 | 3;
  imported: number;
  skipped: number;
  duplicates: number;
  fingerprintSaved: boolean;
  transactions: TransactionSummary[];
}

export interface ImportSummary {
  id: string;
  fileName: string;
  sourceName: string;
  tierUsed: number;
  transactionCount: number;
  createdAt: string;
}

export interface StatementImportListResponse {
  imports: ImportSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface FingerprintSummary {
  id: string;
  sourceName: string;
  isSystem: boolean;
  hitCount: number;
  lastUsedAt: string | null;
  createdAt: string;
}

export interface FingerprintListResponse {
  fingerprints: FingerprintSummary[];
}

export interface TransactionListResponse {
  transactions: TransactionSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
  unmatchedCount: number;
}

export interface TransactionDetail {
  id: string;
  transactionDate: string;
  postDate: string | null;
  description: string;
  originalDescription: string;
  amount: number;
  matchedReceiptId: string | null;
  importId: string;
  importFileName: string;
  createdAt: string;
}

export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
}

// API Error class
export class ApiError extends Error {
  constructor(
    public status: number,
    public title: string,
    public detail?: string
  ) {
    super(detail || title);
    this.name = 'ApiError';
  }
}

// Base API configuration
const API_BASE = '/api';

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const problem: ProblemDetails = await response.json().catch(() => ({
      title: 'Request failed',
      status: response.status,
    }));
    throw new ApiError(problem.status, problem.title, problem.detail);
  }
  return response.json();
}

/**
 * Analyze a statement file to detect column mappings
 */
export async function analyzeStatement(file: File, token: string): Promise<StatementAnalyzeResponse> {
  const formData = new FormData();
  formData.append('file', file);

  const response = await fetch(`${API_BASE}/statements/analyze`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      // Don't set Content-Type for FormData - browser sets it with boundary
    },
    body: formData,
  });

  return handleResponse<StatementAnalyzeResponse>(response);
}

/**
 * Import transactions using confirmed column mapping
 */
export async function importStatement(
  request: StatementImportRequest,
  token: string
): Promise<StatementImportResponse> {
  const response = await fetch(`${API_BASE}/statements/import`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  return handleResponse<StatementImportResponse>(response);
}

/**
 * Get list of recent imports
 */
export async function getImports(
  page: number = 1,
  pageSize: number = 20,
  token: string
): Promise<StatementImportListResponse> {
  const params = new URLSearchParams({
    page: page.toString(),
    pageSize: pageSize.toString(),
  });

  const response = await fetch(`${API_BASE}/statements/imports?${params}`, {
    headers: {
      'Authorization': `Bearer ${token}`,
    },
  });

  return handleResponse<StatementImportListResponse>(response);
}

/**
 * Get list of available fingerprints (user + system)
 */
export async function getFingerprints(token: string): Promise<FingerprintListResponse> {
  const response = await fetch(`${API_BASE}/statements/fingerprints`, {
    headers: {
      'Authorization': `Bearer ${token}`,
    },
  });

  return handleResponse<FingerprintListResponse>(response);
}

/**
 * Get paginated list of transactions
 */
export async function getTransactions(params: {
  page?: number;
  pageSize?: number;
  startDate?: string;
  endDate?: string;
  matched?: boolean;
  importId?: string;
}, token: string): Promise<TransactionListResponse> {
  const searchParams = new URLSearchParams();

  if (params.page) searchParams.set('page', params.page.toString());
  if (params.pageSize) searchParams.set('pageSize', params.pageSize.toString());
  if (params.startDate) searchParams.set('startDate', params.startDate);
  if (params.endDate) searchParams.set('endDate', params.endDate);
  if (params.matched !== undefined) searchParams.set('matched', params.matched.toString());
  if (params.importId) searchParams.set('importId', params.importId);

  const response = await fetch(`${API_BASE}/transactions?${searchParams}`, {
    headers: {
      'Authorization': `Bearer ${token}`,
    },
  });

  return handleResponse<TransactionListResponse>(response);
}

/**
 * Get transaction details by ID
 */
export async function getTransaction(id: string, token: string): Promise<TransactionDetail> {
  const response = await fetch(`${API_BASE}/transactions/${id}`, {
    headers: {
      'Authorization': `Bearer ${token}`,
    },
  });

  return handleResponse<TransactionDetail>(response);
}

/**
 * Delete a transaction
 */
export async function deleteTransaction(id: string, token: string): Promise<void> {
  const response = await fetch(`${API_BASE}/transactions/${id}`, {
    method: 'DELETE',
    headers: {
      'Authorization': `Bearer ${token}`,
    },
  });

  if (!response.ok && response.status !== 204) {
    const problem: ProblemDetails = await response.json().catch(() => ({
      title: 'Delete failed',
      status: response.status,
    }));
    throw new ApiError(problem.status, problem.title, problem.detail);
  }
}

// Column field type options for UI dropdowns
export const COLUMN_FIELD_OPTIONS: { value: ColumnFieldType; label: string; required: boolean }[] = [
  { value: 'date', label: 'Transaction Date', required: true },
  { value: 'post_date', label: 'Post Date', required: false },
  { value: 'description', label: 'Description', required: true },
  { value: 'amount', label: 'Amount', required: true },
  { value: 'category', label: 'Category', required: false },
  { value: 'memo', label: 'Memo', required: false },
  { value: 'reference', label: 'Reference', required: false },
  { value: 'ignore', label: 'Ignore', required: false },
];
