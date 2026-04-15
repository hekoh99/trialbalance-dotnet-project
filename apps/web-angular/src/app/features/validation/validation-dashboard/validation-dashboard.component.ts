import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
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
type AccountType = ClassificationResult['classifiedAs'];
type SortField = 'accountCode' | 'accountName' | 'classifiedAs' | 'confidence' | 'flags';
type SortDirection = 'asc' | 'desc';

const ACCOUNT_TYPES: AccountType[] = [
  'Asset', 'Liability', 'Equity', 'Revenue', 'Expense', 'Unclassified',
];

@Component({
  selector: 'app-validation-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
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

  // Filters — each is a signal the template binds into via [(ngModel)]
  flagFilter = signal<string>('all');            // type of flag (e.g. low_confidence)
  accountTypeFilter = signal<AccountType | 'all'>('all');
  searchTerm = signal<string>('');
  needsReviewOnly = signal<boolean>(false);       // quick filter for rows with flags

  // Sort state
  sortField = signal<SortField>('accountCode');
  sortDirection = signal<SortDirection>('asc');

  private sub?: Subscription;

  // --- Derived views ------------------------------------------------------

  /** Flags grouped by account code so the classifications table can highlight affected rows. */
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

  /** Counts per account type — drives the 6 summary cards. */
  typeCards = computed(() => {
    const r = this.result();
    const summary = r?.summary ?? {};
    const byAccount = this.flagsByAccount();
    return ACCOUNT_TYPES.map(type => {
      const count = summary[type] ?? 0;
      // Rows of this type that have at least one flag
      const flaggedCount = (r?.classifications ?? [])
        .filter(c => c.classifiedAs === type && (byAccount.get(c.accountCode)?.length ?? 0) > 0)
        .length;
      return { type, count, flaggedCount };
    });
  });

  /** Distinct flag types present in the current result — drives the filter dropdown. */
  availableFlagTypes = computed(() => {
    const r = this.result();
    if (!r) return [];
    return Array.from(new Set(r.flaggedItems.map(f => f.type)));
  });

  /** Flagged items after applying the type filter. Used in the top anomaly summary. */
  filteredFlaggedItems = computed(() => {
    const r = this.result();
    if (!r) return [];
    const type = this.flagFilter();
    return type === 'all' ? r.flaggedItems : r.flaggedItems.filter(f => f.type === type);
  });

  /** Classifications after applying type filter + search + needs-review, then sort. */
  visibleClassifications = computed(() => {
    const r = this.result();
    if (!r) return [];

    const typeFilter = this.accountTypeFilter();
    const search = this.searchTerm().trim().toLowerCase();
    const reviewOnly = this.needsReviewOnly();
    const flagsMap = this.flagsByAccount();

    let rows = r.classifications.slice();
    if (typeFilter !== 'all')
      rows = rows.filter(c => c.classifiedAs === typeFilter);
    if (search)
      rows = rows.filter(c =>
        c.accountCode.toLowerCase().includes(search) ||
        c.accountName.toLowerCase().includes(search));
    if (reviewOnly)
      rows = rows.filter(c => (flagsMap.get(c.accountCode)?.length ?? 0) > 0);

    const field = this.sortField();
    const dir = this.sortDirection() === 'asc' ? 1 : -1;
    rows.sort((a, b) => {
      let av: number | string = 0;
      let bv: number | string = 0;
      switch (field) {
        case 'confidence': av = a.confidence; bv = b.confidence; break;
        case 'flags':
          av = flagsMap.get(a.accountCode)?.length ?? 0;
          bv = flagsMap.get(b.accountCode)?.length ?? 0;
          break;
        default: av = (a as any)[field] ?? ''; bv = (b as any)[field] ?? ''; break;
      }
      if (av < bv) return -1 * dir;
      if (av > bv) return 1 * dir;
      return 0;
    });
    return rows;
  });

  constructor(
    private route: ActivatedRoute,
    private validation: ValidationService,
    private signalr: SignalRService,
  ) {}

  ngOnInit() {
    this.engagementId = this.route.snapshot.paramMap.get('id')!;
    this.trialBalanceId = this.route.snapshot.queryParamMap.get('trialBalanceId');

    // Load current job status first so the badge renders before any SignalR event arrives.
    this.validation.getStatus(this.engagementId).subscribe({
      next: (job) => {
        if (job.status !== 'none') {
          this.status.set(job.status);
          this.errorMessage.set(job.errorMessage ?? null);
          if (job.status === 'Completed') this.loadResult();
        }
      },
    });

    // Live updates from Worker → Service Bus → API → SignalR.
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
    if (update.status === 'Completed') this.loadResult();
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

  toggleSort(field: SortField) {
    if (this.sortField() === field) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortField.set(field);
      this.sortDirection.set('asc');
    }
  }

  clearFilters() {
    this.flagFilter.set('all');
    this.accountTypeFilter.set('all');
    this.searchTerm.set('');
    this.needsReviewOnly.set(false);
  }

  // --- Template helpers ---------------------------------------------------

  statusBadgeClass(status: Status): string {
    switch (status) {
      case 'Queued': return 'bg-gray-100 text-gray-700';
      case 'Processing': return 'bg-blue-100 text-blue-800';
      case 'Completed': return 'bg-green-100 text-green-800';
      case 'Failed': return 'bg-red-100 text-red-800';
      default: return 'bg-gray-50 text-gray-500';
    }
  }

  accountTypeBadgeClass(type: AccountType): string {
    switch (type) {
      case 'Asset': return 'bg-emerald-100 text-emerald-800';
      case 'Liability': return 'bg-orange-100 text-orange-800';
      case 'Equity': return 'bg-violet-100 text-violet-800';
      case 'Revenue': return 'bg-sky-100 text-sky-800';
      case 'Expense': return 'bg-rose-100 text-rose-800';
      case 'Unclassified': return 'bg-yellow-100 text-yellow-800';
    }
  }

  accountTypeCardClass(type: AccountType): string {
    // Soft background per type, matches the badge palette.
    switch (type) {
      case 'Asset': return 'bg-emerald-50 border-emerald-200';
      case 'Liability': return 'bg-orange-50 border-orange-200';
      case 'Equity': return 'bg-violet-50 border-violet-200';
      case 'Revenue': return 'bg-sky-50 border-sky-200';
      case 'Expense': return 'bg-rose-50 border-rose-200';
      case 'Unclassified': return 'bg-yellow-50 border-yellow-200';
    }
  }

  sortIndicator(field: SortField): string {
    if (this.sortField() !== field) return '';
    return this.sortDirection() === 'asc' ? '↑' : '↓';
  }

  // Used by [(ngModel)] — signals aren't directly bindable, so these wrappers bridge.
  get flagFilterValue() { return this.flagFilter(); }
  set flagFilterValue(v: string) { this.flagFilter.set(v); }
  get accountTypeFilterValue() { return this.accountTypeFilter(); }
  set accountTypeFilterValue(v: AccountType | 'all') { this.accountTypeFilter.set(v); }
  get searchTermValue() { return this.searchTerm(); }
  set searchTermValue(v: string) { this.searchTerm.set(v); }
  get needsReviewOnlyValue() { return this.needsReviewOnly(); }
  set needsReviewOnlyValue(v: boolean) { this.needsReviewOnly.set(v); }

  readonly accountTypes = ACCOUNT_TYPES;
}
