import { Component, Input } from '@angular/core';
import { MainActivity, Running } from '../../../../_models/ActivityClasses';
@Component({
  selector: 'app-running-activity',
  standalone: false,
  templateUrl: './running-activity.component.html',
  styleUrl: './running-activity.component.css'
})

export class RunningActivityComponent {
  constructor(public activity: Running) { } //Injector

  ngOnInit() {
    console.log('activity', this.activity); // Check if the activity object is populated
  }
}
