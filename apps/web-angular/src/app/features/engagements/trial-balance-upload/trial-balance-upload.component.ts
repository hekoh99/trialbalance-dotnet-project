import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EngagementService } from '../../../core/services/engagement.service';

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
  uploadResult = signal<any>(null);
  error = signal<string | null>(null);

  constructor(private engagementService: EngagementService) {}

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

    this.engagementService.uploadTrialBalance(this.engagementId, file).subscribe({
      next: (result) => {
        this.uploadResult.set(result);
        this.uploading.set(false);
        this.uploadComplete.emit();
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Upload failed. Please try again.');
        this.uploading.set(false);
      },
    });
  }
}
