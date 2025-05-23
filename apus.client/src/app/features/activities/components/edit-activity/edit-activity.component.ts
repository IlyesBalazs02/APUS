import { Component } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { FormlyFieldConfig, FormlyFormOptions } from '@ngx-formly/core';
import { selectActivityHelper } from '../create-activity/formly/selectActivityHelper';
import { HttpClient } from '@angular/common/http';
import { MainActivity, createActivity } from '../../_models/ActivityClasses';
import { mainFields } from '../create-activity/formly/formFieldConfigs';
import { ActivatedRoute } from '@angular/router';
import { catchError, throwError } from 'rxjs';

@Component({
  selector: 'app-edit-activity',
  standalone: false,
  templateUrl: './edit-activity.component.html',
  styleUrls: ['./edit-activity.component.css'],
})
export class EditActivityComponent {
  activityId: string;
  form = new FormGroup({});
  model: any = {};
  options: FormlyFormOptions = {};
  fields: FormlyFieldConfig[] = [];
  selectActivityHelper = new selectActivityHelper();

  /** Cache of models per activityType */
  private models: Record<string, any> = {};

  constructor(private route: ActivatedRoute, private http: HttpClient) {
    this.activityId = this.route.snapshot.paramMap.get('id')!;
  }

  ngOnInit() {
    this.getActivities();
  }

  private getActivities() {
    this.http
      .get<MainActivity>(`/api/activities/${this.activityId}`)
      .subscribe(dto => {
        const activity = createActivity(dto);

        let dateOnly = '';
        if (dto.date) {
          dateOnly = new Date(dto.date).toISOString().substring(0, 10);
        }

        this.selectActivityHelper.selectedActivity = activity;
        this.models[activity.activityType] = { ...activity, date: dateOnly };
        this.buildFormFor(activity.activityType);
      }, err => console.error(err));
  }

  onActivityChange(selection: MainActivity) {
    const newType = selection.activityType;

    const dto: any = {
      ...this.model,
      activityType: newType,
      $type: selection.$type,
    };

    const transformed = createActivity(dto);

    this.selectActivityHelper.selectedActivity = transformed;

    this.fields = [
      ...mainFields,
      ...this.selectActivityHelper.subtypeMap[newType],
    ];

    this.model = transformed;
    this.form.reset(this.model);
  }

  private buildFormFor(type: string) {
    this.fields = [
      ...mainFields,
      ...this.selectActivityHelper.subtypeMap[type],
    ];
    this.model = this.models[type] || { ...this.selectActivityHelper.selectedActivity };
    this.form.reset(this.model);
  }

  submit() {
    const payload = { ...this.model };
    console.log('Submitting', payload);

    this.http
      .put<void>(`/api/activities/${this.activityId}`, payload)
      .pipe(
        catchError(err => {
          console.error('Update failed', err);
          return throwError(() => err);
        })
      )
      .subscribe({
        next: () => {
          console.log('Activity updated successfully');
        },
        error: () => {
          alert('There was an error saving your changes.');
        },
      });

  }
}
