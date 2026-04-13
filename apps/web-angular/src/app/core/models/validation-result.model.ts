export interface ValidationResult {
  engagementId: string;
  trialBalanceId: string;
  isBalanced: boolean;
  totalDebits: number;
  totalCredits: number;
  variance: number;
  classifications: ClassificationResult[];
  summary: Record<string, number>;
  flaggedItems: FlaggedItem[];
}

export interface ClassificationResult {
  accountCode: string;
  accountName: string;
  classifiedAs: string;
  confidence: number;
  flags: FlaggedItem[];
  reasoning?: string;
}

export interface FlaggedItem {
  type: string;
  message: string;
  accountCode?: string;
}

export interface ValidationStatus {
  status: 'Queued' | 'Processing' | 'Completed' | 'Failed';
  engagementId: string;
}
