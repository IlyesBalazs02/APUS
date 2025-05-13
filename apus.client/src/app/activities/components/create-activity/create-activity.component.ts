import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { FormlyFieldConfig, FormlyFormOptions } from '@ngx-formly/core';
import { mainFields } from './formly/formFieldConfigs';
import { selectActivityHelper } from './formly/selectActivityHelper';
import { MainActivity, Running } from '../../_models/ActivityClasses';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-create-activity',
  standalone: false,
  templateUrl: './create-activity.component.html',
  styleUrls: ['./create-activity.component.scss'],
})

export class CreateActivityComponent implements OnInit {
  form = new FormGroup({});
  model: any = {};
  options: FormlyFormOptions = {};
  fields: FormlyFieldConfig[] = [];
  selectActivityHelper = new selectActivityHelper();

  // Drag & Drop state
  isDragOver = false;
  files: File[] = [];
  previewUrls: string[] = [];

  constructor(private http: HttpClient) { }
  ngOnInit() {
    this.updateFields(this.selectActivityHelper.selectedActivity);
  }

  onActivityChange(activity: MainActivity) {
    this.updateFields(activity);
    this.selectActivityHelper.selectedActivity = activity;
  }

  private updateFields(activity: MainActivity) {
    const key = activity.activityType;
    const extras = this.selectActivityHelper.subtypeMap[key] || [];

    // merge main + subtype, then reset form & model
    this.fields = [
      ...mainFields,
      ...extras,
    ];
    this.model = {};
    this.form.reset();
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

  onFileSelected(event: Event) {
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

  submit() {
    const formData = { ...this.model };

    const payload = {
      $type: this.selectActivityHelper.selectedActivity.$type,
      ...formData
    };

    console.log(JSON.stringify(payload));

    this.http.post<MainActivity>('/api/activities', payload)
      .subscribe(createdActivity => {

        if (this.files.length === 0) return;

        const activityId = createdActivity.id;

        const formData = new FormData();
        this.files.forEach(f => formData.append('images', f));

        this.http.post(`/api/images/${createdActivity.id}/images`, formData)
          .subscribe(() => {
            console.log('Pictures uploaded!');
          }, err => {
            console.log('Error uploading pictures', err);
          });

      }, err => {
        console.log('Error creating activity', err);
      });
  }
}

