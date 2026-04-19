import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ValidationResult } from '../models/validation-result.model';
import { environment } from '../../../environments/environment';

export interface ValidationJob {
  id: string;
  engagementId: string;
  trialBalanceId: string;
  status: 'Queued' | 'Processing' | 'Completed' | 'Failed' | 'none';
  createdAt: string;
  updatedAt: string;
  completedAt: string | null;
  errorMessage: string | null;
}

@Injectable({ providedIn: 'root' })
export class ValidationService {
  private readonly apiBase = `${environment.apiUrl}/api/engagements`;

  constructor(private http: HttpClient) {}

  /** Kick off a validation run for the given trial balance. */
  trigger(engagementId: string, trialBalanceId: string): Observable<ValidationJob> {
    return this.http.post<ValidationJob>(`${this.apiBase}/${engagementId}/validate`, {
      trialBalanceId,
    });
  }

  /** Latest validation_job status snapshot — used on dashboard load. */
  getStatus(engagementId: string): Observable<ValidationJob> {
    return this.http.get<ValidationJob>(`${this.apiBase}/${engagementId}/status`);
  }

  /** Classification result document from Cosmos (once status = Completed). */
  getResult(engagementId: string): Observable<ValidationResult> {
    return this.http.get<ValidationResult>(`${this.apiBase}/${engagementId}/validation`);
  }
}
