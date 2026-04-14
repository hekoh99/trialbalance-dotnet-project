export interface Engagement {
  id: string;
  clientName: string;
  fiscalYearEnd: string;
  createdAt: string;
  trialBalances: TrialBalanceSummary[];
}

export interface TrialBalanceSummary {
  id: string;
  fileName: string;
  submittedAt: string;
  totalDebits: number;
  totalCredits: number;
  isBalanced: boolean;
  glEntryCount: number;
}

export interface CreateEngagementRequest {
  clientName: string;
  fiscalYearEnd: string;
}

/** Response returned by POST /api/engagements/{id}/trial-balance. */
export interface TrialBalanceUploadResult {
  id: string;
  engagementId: string;
  fileName: string;
  submittedAt: string;
  totalDebits: number;
  totalCredits: number;
  isBalanced: boolean;
  glEntryCount: number;
}
