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
  styleUrls: ['./upload-activity.component.css'],
})
export class UploadActivityComponent {
  selectedFile: File | null = null;
  form = new FormGroup({});

  constructor(private http: HttpClient, private router: Router) { }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length) {
      this.selectedFile = input.files[0];
    }
  }

  submit(): void {
    const formData = new FormData();
    if (this.selectedFile) {
      formData.append('trackFile', this.selectedFile, this.selectedFile.name);
    }

    // Send to the upload endpoint
    this.http.post<UploadResponse>('/api/uploadactivity/upload-activity', formData)
      .subscribe(response => {
        //('Upload success', response);
        this.router.navigate(['/DisplayActivity', response.id]);

      }, error => {
        console.error('Upload error', error);
      });
  }
}
