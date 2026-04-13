import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { EngagementService } from '../../../core/services/engagement.service';
import { Engagement } from '../../../core/models/engagement.model';
import { TrialBalanceUploadComponent } from '../trial-balance-upload/trial-balance-upload.component';

@Component({
  selector: 'app-engagement-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, TrialBalanceUploadComponent],
  templateUrl: './engagement-detail.component.html',
})
export class EngagementDetailComponent implements OnInit {
  engagement = signal<Engagement | null>(null);
  loading = signal(true);
  engagementId = '';

  constructor(
    private route: ActivatedRoute,
    private engagementService: EngagementService,
  ) {}

  ngOnInit() {
    this.engagementId = this.route.snapshot.paramMap.get('id')!;
    this.loadEngagement();
  }

  loadEngagement() {
    this.loading.set(true);
    this.engagementService.get(this.engagementId).subscribe({
      next: (data) => {
        this.engagement.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onUploadComplete() {
    this.loadEngagement();
  }
}
