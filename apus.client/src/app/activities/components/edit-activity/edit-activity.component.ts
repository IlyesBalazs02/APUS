import { Component } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { FormlyFieldConfig, FormlyFormOptions } from '@ngx-formly/core';
import { selectActivityHelper } from '../create-activity/formly/selectActivityHelper';
import { HttpClient } from '@angular/common/http';
import { MainActivity, createActivity } from '../../_models/ActivityClasses';
import { mainFields } from '../create-activity/formly/formFieldConfigs';
import { ActivatedRoute } from '@angular/router';

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
        // Store the loaded DTO as the initial model for that type
        this.selectActivityHelper.selectedActivity = activity;
        this.models[activity.activityType] = { ...activity };
        this.buildFormFor(activity.activityType);
      }, err => console.error(err));
  }

  /** Called whenever the dropdown selection changes */
  onActivityChange(selection: MainActivity) {
    // 1) Figure out the new type string
    const newType = selection.activityType;

    // 2) Build a DTO merging your current form values with the new type/$type
    const dto: any = {
      ...this.model,
      activityType: newType,
      $type: selection.$type,
    };

    // 3) Call createActivity to give you back a real instance of the right subclass
    const transformed = createActivity(dto);                                   // :contentReference[oaicite:0]{index=0}:contentReference[oaicite:1]{index=1}

    // 4) Swap it in as the selectedActivity
    this.selectActivityHelper.selectedActivity = transformed;

    // 5) Rebuild your Formly fields for the new type
    this.fields = [
      ...mainFields,
      ...this.selectActivityHelper.subtypeMap[newType],                       // :contentReference[oaicite:2]{index=2}:contentReference[oaicite:3]{index=3}
    ];

    // 6) Replace your form model & reset the form so the new subclass’s
    //    default-constructed properties (and any overlapping old ones) appear
    this.model = transformed;
    this.form.reset(this.model);
  }

  /** Assemble fields & model for a given activityType */
  private buildFormFor(type: string) {
    // Base fields + subtype‐specific fields
    this.fields = [
      ...mainFields,
      ...this.selectActivityHelper.subtypeMap[type],
    ];
    // Restore previous model or start from the loaded DTO
    this.model = this.models[type] || { ...this.selectActivityHelper.selectedActivity };
    // If you want, you can also reset validations:
    this.form.reset(this.model);
  }

  submit() {
    const payload = { ...this.model, activityType: this.selectActivityHelper.selectedActivity.activityType };
    console.log('Submitting', payload);
    // this.http.post(`/api/activities/${this.activityId}`, payload).subscribe(...);
  }
}
