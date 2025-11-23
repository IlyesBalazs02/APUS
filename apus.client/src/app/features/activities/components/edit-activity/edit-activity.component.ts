import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { EditActivityDto } from './EditActivityDto';
import * as ExifReader from 'exifreader';
import { forkJoin, Observable } from 'rxjs';
import { ExifService } from '../services/ExifService';

@Component({
  selector: 'app-edit-activity',
  standalone: false,
  templateUrl: './edit-activity.component.html',
  styleUrls: ['./edit-activity.component.css'],
})
export class EditActivityComponent {
  activityId: string;

  // Existing images from server (these are the ones you can delete)
  images: string[] = [];
  // Filenames of images to delete (NOT full URLs)
  imagesMarkedForDelete = new Set<string>();

  // Newly added images
  newFiles: File[] = [];
  newPreviewUrls: string[] = [];
  isDragOver = false;
  exifDataMap: Map<string, any> = new Map();

  editModel: EditActivityDto = {
    id: '',
    title: '',
    description: '',
    date: '',
    activityType: ''
  };

  activityTypes = [
    { value: 'MainActivity', label: 'Activity' },
    { value: 'Running', label: 'Running' },
    { value: 'Hiking', label: 'Hiking' },
    { value: 'Cycling', label: 'Cycling' },
    { value: 'GpsRelatedActivity', label: 'Gps-related' },
  ];

  constructor(
    private route: ActivatedRoute,
    private http: HttpClient,
    private router: Router,
    private exifService: ExifService
  ) {
    this.activityId = this.route.snapshot.paramMap.get('id')!;
  }

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      this.activityId = params.get('id')!;
      this.loadActivity();
      this.loadImages();
    });
  }

  // --------- Activity data ---------

  private loadActivity() {
    this.http.get<any>(`/api/activities/${this.activityId}`).subscribe(dto => {
      const d = dto.date ? new Date(dto.date).toISOString().substring(0, 10) : '';

      this.editModel = {
        id: dto.id,
        title: dto.title,
        description: dto.description,
        date: d,
        activityType: dto.type
      };
    });
  }

  // --------- Existing images from server ---------

  private loadImages() {
    this.http.get<string[]>(`/api/images/${this.activityId}/urls`)
      .subscribe({
        next: (files) => {
          if (!files || !Array.isArray(files)) {
            this.images = [];
            return;
          }
          this.images = files;
        },
        error: (err) => {
          console.warn('No images found or API returned error, skipping.', err);
          this.images = [];
        }
      });
  }

  // User clicks "X" on an existing image and that marks that file for deletion
  markImageForDeletion(index: number) {
    const url = this.images[index];
    if (!url) return;

    const fileName = this.extractFileName(url);
    this.imagesMarkedForDelete.add(fileName);

    this.images.splice(index, 1);
  }

  private extractFileName(url: string): string {
    try {
      // Absolute URL
      const u = new URL(url);
      const last = u.pathname.split('/').pop();
      return last ?? url;
    } catch {
      // Relative path
      const last = url.split('/').pop();
      return last ?? url;
    }
  }

  // --------- New images (upload-like editor) ---------

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

  private async handleFiles(files: File[]) {
    const images = files.filter(f => f.type.startsWith('image/'));

    // Extract EXIF centrally
    const exifMap = await this.exifService.extractMany(images);

    for (const file of images) {
      this.newFiles.push(file);
      this.exifDataMap.set(file.name, exifMap.get(file.name) ?? {});

      // Preview logic stays the same:
      const reader = new FileReader();
      reader.onload = (e: ProgressEvent<FileReader>) => {
        if (e.target?.result) {
          this.newPreviewUrls.push(e.target.result as string);
        }
      };
      reader.readAsDataURL(file);
    }
  }


  removeNewImage(index: number) {
    const file = this.newFiles[index];
    if (file) {
      this.exifDataMap.delete(file.name);
    }

    this.newFiles.splice(index, 1);
    this.newPreviewUrls.splice(index, 1);
  }

  // --------- Submit: save activity + sync images ---------

  submit(form: any) {
    if (form.invalid) {
      return;
    }

    const payload: EditActivityDto = {
      ...this.editModel,
      id: this.activityId,
    };

    this.http.put<void>(`/api/activities/${this.activityId}`, payload)
      .subscribe({
        next: () => {
          this.syncImagesBatch();
        },
        error: err => {
          console.error(err);
          alert('Failed to save changes.');
        }
      });
  }


  private syncImagesBatch() {
    const requests: Observable<any>[] = [];

    // delete multiple image
    if (this.imagesMarkedForDelete.size > 0) {
      const filesToDelete = Array.from(this.imagesMarkedForDelete);
      requests.push(
        this.http.post(
          `/api/images/${this.activityId}/images/delete`,
          filesToDelete,
          { withCredentials: true }
        )
      );
    }

    // Upload new images
    if (this.newFiles.length > 0) {
      const formData = new FormData();
      this.newFiles.forEach(f => formData.append('images', f));

      const exifJson = JSON.stringify(Object.fromEntries(this.exifDataMap.entries()));
      formData.append('exifJson', exifJson);

      requests.push(
        this.http.post(
          `/api/images/${this.activityId}/images`,
          formData,
          { withCredentials: true }
        )
      );
    }

    // Nothing to sync
    if (!requests.length) {
      this.router.navigate(['/activities', this.activityId]);
      return;
    }

    forkJoin(requests).subscribe({
      next: () => {
        this.router.navigate(['/activities', this.activityId]);
      },
      error: err => {
        console.error('Error updating images', err);

        this.router.navigate(['/activities', this.activityId]);
      }
    });
  }
}
