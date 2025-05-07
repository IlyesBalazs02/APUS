import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { FormGroup } from '@angular/forms';

@Component({
  selector: 'app-upload-gpx-file',
  standalone: false,
  templateUrl: './upload-gpx-file.component.html',
  styleUrl: './upload-gpx-file.component.css'
})
export class UploadGpxFileComponent {
  selectedFile: File | null = null;
  form = new FormGroup({});

  constructor(private http: HttpClient) { }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length) {
      this.selectedFile = input.files[0];
    }
  }

  submit(): void {
    // First, create FormData
    const formData = new FormData();
    if (this.selectedFile) {
      formData.append('gpxFile', this.selectedFile, this.selectedFile.name);
    }

    // Append any other activity metadata if needed
    // formData.append('title', this.form.value.title);

    // Send to the upload endpoint
    this.http.post('/api/gpx/upload-gpx', formData)
      .subscribe(response => {
        console.log('Upload success', response);
        // Optionally proceed to create activity metadata
      }, error => {
        console.error('Upload error', error);
      });
  }
}
