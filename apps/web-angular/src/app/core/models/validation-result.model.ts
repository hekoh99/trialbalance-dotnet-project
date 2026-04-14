export interface ValidationResult {
  id: string;
  engagementId: string;
  isBalanced: boolean;
  totalDebits: number;
  totalCredits: number;
  variance: number;
  classifications: ClassificationResult[];
  summary: Record<string, number>;
  flaggedItems: FlaggedItem[];
  processedAt: string;
}

export interface ClassificationResult {
  accountCode: string;
  accountName: string;
  classifiedAs: 'Asset' | 'Liability' | 'Equity' | 'Revenue' | 'Expense' | 'Unclassified';
  confidence: number;
  flags: FlaggedItem[];
  reasoning?: string;
}

export interface FlaggedItem {
  type: string;
  severity?: string;
  detail?: string;
  accountCode?: string;
  accountName?: string;
  [key: string]: unknown;
}

export interface ValidationStatus {
  engagementId: string;
  validationJobId: string;
  status: 'Queued' | 'Processing' | 'Completed' | 'Failed';
  errorMessage?: string | null;
  timestamp: string;
}
