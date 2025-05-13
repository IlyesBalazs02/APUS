import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { Router } from '@angular/router';

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

  // Drag & Drop state
  isDragOver = false;
  files: File[] = [];
  previewUrls: string[] = [];

  constructor(private http: HttpClient, private router: Router) { }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length) {
      this.selectedFile = input.files[0];
    }
  }

  // Drag & Drop handlers
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

  private handleFiles(files: File[]) {
    const images = files.filter(f => f.type.startsWith('image/'));
    images.forEach(file => {
      this.files.push(file);
      const reader = new FileReader();
      reader.onload = (e: ProgressEvent<FileReader>) => {
        if (e.target?.result) {
          this.previewUrls.push(e.target.result as string);
        }
      };
      reader.readAsDataURL(file);
    });

  }

  removeImage(i: number) {
    this.previewUrls.splice(i, 1);

    this.files.splice(i, 1);
  }

  submit(): void {
    const formData = new FormData();
    if (this.selectedFile) {
      formData.append('trackFile', this.selectedFile, this.selectedFile.name);
    }

    this.http.post<UploadResponse>('/api/activityfile/upload-activity', formData)
      .subscribe(response => {

        if (this.files.length === 0) return;

        const activityId = response.id;

        const formData = new FormData();
        this.files.forEach(f => formData.append('images', f));

        this.http.post(`/api/images/${response.id}/images`, formData)
          .subscribe(() => {
            console.log('Pictures uploaded!');
            this.router.navigate(['/DisplayActivity', response.id]);
          }, err => {
            console.log('Error uploading pictures', err);
          });
      }, error => {
        console.error('Upload error', error);
      });
  }
}

