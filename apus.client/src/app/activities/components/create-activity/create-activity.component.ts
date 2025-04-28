import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { FormlyFieldConfig, FormlyFormOptions } from '@ngx-formly/core';
import { mainFields } from '../../formly/main.fields';
import { selectActivityHelper } from './selectActivityHelper';
import { MainActivity } from '../../_models/ActivityClasses';

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
  selectActivity = new selectActivityHelper();

  ngOnInit() {
    this.updateFields(this.selectActivity.selectedActivity);
  }

  onActivityChange(activity: MainActivity) {
    this.updateFields(activity);
  }

  private updateFields(activity: MainActivity) {
    const key = activity.displayName;   // “Running”, “Bouldering”, etc.
    const extras = this.selectActivity.subtypeMap[key] || [];

    // merge main + subtype, then reset form & model
    this.fields = [
      ...mainFields,
      ...extras,
    ];
    this.model = {};
    this.form.reset();
  }
}
