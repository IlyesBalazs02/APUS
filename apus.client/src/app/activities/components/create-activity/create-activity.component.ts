import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { FormlyFieldConfig, FormlyFormOptions } from '@ngx-formly/core';
import { mainFields } from './formly/formFieldConfigs';
import { selectActivityHelper } from './formly/selectActivityHelper';
import { MainActivity } from '../../_models/ActivityClasses';
import { HttpClient } from '@angular/common/http';

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
  tmp?: MainActivity; //deletelater

  constructor(private http: HttpClient) { }
  ngOnInit() {
    this.updateFields(this.selectActivityHelper.selectedActivity);
    this.tmp = new MainActivity();
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

  submit() {
    const formData = { ...this.model };

    const payload = {
      ...formData,
      activityType: this.selectActivityHelper.selectedActivity.$type
    };

    console.log(JSON.stringify(payload));
    console.log(JSON.stringify(this.tmp))

    this.http.post('/api/activities', payload).subscribe(() => {
      console.log('Submitted running activity!');
    });
  }
}
