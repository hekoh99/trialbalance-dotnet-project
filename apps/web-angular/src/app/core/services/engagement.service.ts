import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Engagement,
  CreateEngagementRequest,
  TrialBalanceUploadResult,
} from '../models/engagement.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class EngagementService {
  private readonly apiUrl = `${environment.apiUrl}/api/engagements`;

  constructor(private http: HttpClient) {}

  list(): Observable<Engagement[]> {
    return this.http.get<Engagement[]>(this.apiUrl);
  }

  get(id: string): Observable<Engagement> {
    return this.http.get<Engagement>(`${this.apiUrl}/${id}`);
  }

  create(request: CreateEngagementRequest): Observable<Engagement> {
    return this.http.post<Engagement>(this.apiUrl, request);
  }

  uploadTrialBalance(
    engagementId: string,
    file: File,
  ): Observable<TrialBalanceUploadResult> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<TrialBalanceUploadResult>(
      `${this.apiUrl}/${engagementId}/trial-balance`,
      formData,
    );
  }
}
