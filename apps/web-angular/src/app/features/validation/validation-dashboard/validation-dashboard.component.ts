import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';

import { ValidationService, ValidationJob } from '../../../core/services/validation.service';
import { SignalRService } from '../../../core/services/signalr.service';
import {
  ValidationResult,
  ValidationStatus,
  FlaggedItem,
  ClassificationResult,
} from '../../../core/models/validation-result.model';

type Status = ValidationJob['status'] | 'Unknown';

@Component({
  selector: 'app-validation-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './validation-dashboard.component.html',
})
export class ValidationDashboardComponent implements OnInit, OnDestroy {
  engagementId = '';
  trialBalanceId: string | null = null;

  status = signal<Status>('Unknown');
  errorMessage = signal<string | null>(null);
  result = signal<ValidationResult | null>(null);
  loadingResult = signal(false);
  retrying = signal(false);

  private sub?: Subscription;

  // Derived helpers for the template
  flagsByAccount = computed(() => {
    const r = this.result();
    if (!r) return new Map<string, FlaggedItem[]>();
    const map = new Map<string, FlaggedItem[]>();
    for (const item of r.flaggedItems) {
      const key = item.accountCode ?? '';
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(item);
    }
    return map;
  });

  summaryEntries = computed(() => {
    const s = this.result()?.summary ?? {};
    return Object.entries(s);
  });

  constructor(
    private route: ActivatedRoute,
    private validation: ValidationService,
    private signalr: SignalRService,
  ) {}

  ngOnInit() {
    this.engagementId = this.route.snapshot.paramMap.get('id')!;
    this.trialBalanceId = this.route.snapshot.queryParamMap.get('trialBalanceId');

    // Load whatever status is already recorded in Postgres so the badge renders
    // before any SignalR event arrives (e.g. user refreshes mid-run).
    this.validation.getStatus(this.engagementId).subscribe({
      next: (job) => {
        if (job.status !== 'none') {
          this.status.set(job.status);
          this.errorMessage.set(job.errorMessage ?? null);
          if (job.status === 'Completed') this.loadResult();
        }
      },
    });

    // Live updates from the Worker pipeline via Service Bus → API → SignalR.
    this.sub = this.signalr.connect(this.engagementId).subscribe({
      next: (update: ValidationStatus) => this.onStatusUpdate(update),
      error: (err) => console.error('SignalR connection error', err),
    });
  }

  ngOnDestroy() {
    this.sub?.unsubscribe();
  }

  private onStatusUpdate(update: ValidationStatus) {
    this.status.set(update.status);
    this.errorMessage.set(update.errorMessage ?? null);
    if (update.status === 'Completed') {
      this.loadResult();
    }
  }

  private loadResult() {
    this.loadingResult.set(true);
    this.validation.getResult(this.engagementId).subscribe({
      next: (r) => {
        this.result.set(r);
        this.loadingResult.set(false);
      },
      error: () => this.loadingResult.set(false),
    });
  }

  retry() {
    if (!this.trialBalanceId) return;
    this.retrying.set(true);
    this.status.set('Queued');
    this.errorMessage.set(null);
    this.result.set(null);

    this.validation.trigger(this.engagementId, this.trialBalanceId).subscribe({
      next: () => this.retrying.set(false),
      error: (err) => {
        this.retrying.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Retry failed');
      },
    });
  }

  // Template helpers
  statusBadgeClass(status: Status): string {
    switch (status) {
      case 'Queued': return 'bg-gray-100 text-gray-700';
      case 'Processing': return 'bg-blue-100 text-blue-800';
      case 'Completed': return 'bg-green-100 text-green-800';
      case 'Failed': return 'bg-red-100 text-red-800';
      default: return 'bg-gray-50 text-gray-500';
    }
  }

  accountTypeBadgeClass(type: ClassificationResult['classifiedAs']): string {
    switch (type) {
      case 'Asset': return 'bg-emerald-100 text-emerald-800';
      case 'Liability': return 'bg-orange-100 text-orange-800';
      case 'Equity': return 'bg-violet-100 text-violet-800';
      case 'Revenue': return 'bg-sky-100 text-sky-800';
      case 'Expense': return 'bg-rose-100 text-rose-800';
      case 'Unclassified': return 'bg-yellow-100 text-yellow-800';
    }
  }
}
