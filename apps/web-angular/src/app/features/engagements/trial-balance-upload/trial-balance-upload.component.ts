import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { EngagementService } from '../../../core/services/engagement.service';
import { ValidationService } from '../../../core/services/validation.service';
import { TrialBalanceUploadResult } from '../../../core/models/engagement.model';

@Component({
  selector: 'app-trial-balance-upload',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './trial-balance-upload.component.html',
})
export class TrialBalanceUploadComponent {
  @Input({ required: true }) engagementId!: string;
  @Output() uploadComplete = new EventEmitter<void>();

  dragging = signal(false);
  uploading = signal(false);
  triggering = signal(false);
  uploadResult = signal<TrialBalanceUploadResult | null>(null);
  error = signal<string | null>(null);

  constructor(
    private engagementService: EngagementService,
    private validationService: ValidationService,
    private router: Router,
  ) {}

  onDragOver(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.dragging.set(true);
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.dragging.set(false);
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.dragging.set(false);

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.uploadFile(files[0]);
    }
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.uploadFile(input.files[0]);
    }
  }

  private uploadFile(file: File) {
    if (!file.name.endsWith('.csv')) {
      this.error.set('Please upload a CSV file.');
      return;
    }

    this.uploading.set(true);
    this.error.set(null);
    this.uploadResult.set(null);

    // Upload → parse → (server 201) → auto-trigger validation → navigate to
    // dashboard. The dashboard opens the SignalR connection and renders live
    // Queued / Processing / Completed | Failed badge, so the user never has to
    // click a separate "Validate" button (aligns with the "automation" value).
    this.engagementService.uploadTrialBalance(this.engagementId, file).subscribe({
      next: (result) => {
        this.uploadResult.set(result);
        this.uploading.set(false);
        this.uploadComplete.emit();
        this.triggerValidation(result.id);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Upload failed. Please try again.');
        this.uploading.set(false);
      },
    });
  }

  private triggerValidation(trialBalanceId: string) {
    this.triggering.set(true);
    this.validationService.trigger(this.engagementId, trialBalanceId).subscribe({
      next: () => {
        this.triggering.set(false);
        this.router.navigate(
          ['/engagements', this.engagementId, 'validation'],
          { queryParams: { trialBalanceId } },
        );
      },
      error: (err) => {
        this.triggering.set(false);
        this.error.set(
          err.error?.message ?? 'Validation could not be started. Try again from the engagement page.',
        );
      },
    });
  }
}
