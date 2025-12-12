import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { Router } from '@angular/router';
import * as ExifReader from 'exifreader';
import { ExifService } from '../services/ExifService';

interface UploadResponse {
  id: number;
  fileName: string;
  relativePath: string;
}

@Component({
  selector: 'app-upload-activity',
  standalone: false,
  templateUrl: './upload-activity.component.html',
  styleUrls: ['./upload-activity.component.scss'],
})
export class UploadActivityComponent {
  selectedFile: File | null = null;
  form = new FormGroup({});

  isDragOver = false;
  files: File[] = [];
  previewUrls: string[] = [];

  isUploadingTrack = false;
  isUploadingImages = false;
  uploadMessage = '';

  activityTypes = [
    { value: 'MainActivity', label: 'Activity' },
    { value: 'Running', label: 'Running' },
    { value: 'Hiking', label: 'Hiking' },
    { value: 'Cycling', label: 'Cycling' },
    { value: 'GpsRelatedActivity', label: 'Gps-related' },
  ];

  selectedActivityType = 'GpsRelatedActivity';

  constructor(
    private http: HttpClient,
    private router: Router,
    private exifService: ExifService
  ) { }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length) {
      this.selectedFile = input.files[0];
    }
  }

  onActivityTypeChange(event: Event) {
    const select = event.target as HTMLSelectElement;
    this.selectedActivityType = select.value;
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
    this.isDragOver = true;
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    this.isDragOver = false;
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    this.isDragOver = false;
    const droppedFiles = Array.from(event.dataTransfer?.files || []);
    this.handleFiles(droppedFiles);
  }

  onImageFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const selected = Array.from(input.files || []);
    this.handleFiles(selected);
  }

  exifDataMap: Map<string, any> = new Map();

  private async handleFiles(files: File[]) {
    const images = files.filter(f => f.type.startsWith('image/'));

    const exifMap = await this.exifService.extractMany(images);

    for (const file of images) {
      this.files.push(file);

      const meta = exifMap.get(file.name);
      if (meta?.dateTaken) {
        this.exifDataMap.set(file.name, meta);
      }

      const reader = new FileReader();
      reader.onload = (e: ProgressEvent<FileReader>) => {
        if (e.target?.result) {
          this.previewUrls.push(e.target.result as string);
        }
      };
      reader.readAsDataURL(file);
    }
  }

  removeImage(i: number) {
    this.previewUrls.splice(i, 1);
    this.files.splice(i, 1);
  }

  submit(): void {
    if (!this.selectedFile) {
      return;
    }

    const formData = new FormData();
    formData.append('trackFile', this.selectedFile, this.selectedFile.name);

    formData.append('activityType', this.selectedActivityType);

    this.isUploadingTrack = true;
    this.isUploadingImages = false;
    this.uploadMessage = 'Uploading track...';

    this.http.post<UploadResponse>(
      '/api/activityfile/upload-activity',
      formData,
      { withCredentials: true }
    ).subscribe({
      next: (response) => {
        this.isUploadingTrack = false;

        const activityId = response.id;

        if (this.files.length === 0) {
          this.uploadMessage = '';
          this.router.navigate(['/activities', activityId]);
          return;
        }

        this.isUploadingImages = true;
        this.uploadMessage = 'Uploading images...';

        const imgFormData = new FormData();
        this.files.forEach(f => imgFormData.append('images', f));

        const exifJson = JSON.stringify(Object.fromEntries(this.exifDataMap.entries()));
        imgFormData.append('exifJson', exifJson);

        this.http.post(
          `/api/images/${activityId}/images`,
          imgFormData,
          { withCredentials: true }
        ).subscribe({
          next: () => {
            this.isUploadingImages = false;
            this.uploadMessage = '';
            this.router.navigate(['/activities', activityId]);
          },
          error: err => {
            this.isUploadingImages = false;
            this.uploadMessage = 'Error uploading images.';
            console.error('Error uploading pictures', err);
          }
        });
      },
      error: error => {
        this.isUploadingTrack = false;
        this.uploadMessage = 'Error uploading track.';
        console.error('Upload error', error);
      }
    });
  }
}
