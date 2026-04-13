import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { EngagementService } from '../../../core/services/engagement.service';
import { Engagement } from '../../../core/models/engagement.model';

@Component({
  selector: 'app-engagement-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './engagement-list.component.html',
})
export class EngagementListComponent implements OnInit {
  engagements = signal<Engagement[]>([]);
  loading = signal(true);
  showCreateForm = signal(false);
  newClientName = '';
  newFiscalYearEnd = '';

  constructor(private engagementService: EngagementService) {}

  ngOnInit() {
    this.loadEngagements();
  }

  loadEngagements() {
    this.loading.set(true);
    this.engagementService.list().subscribe({
      next: (data) => {
        this.engagements.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  createEngagement() {
    if (!this.newClientName || !this.newFiscalYearEnd) return;

    this.engagementService.create({
      clientName: this.newClientName,
      fiscalYearEnd: this.newFiscalYearEnd,
    }).subscribe({
      next: () => {
        this.newClientName = '';
        this.newFiscalYearEnd = '';
        this.showCreateForm.set(false);
        this.loadEngagements();
      },
    });
  }
}
