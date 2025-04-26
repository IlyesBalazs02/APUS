import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { MainActivity, Running, Bouldering } from '../../_models/ActivityClasses';

@Component({
  selector: 'app-activity-selector',
  standalone: false,
  templateUrl: './activity-selector.component.html',
  styleUrl: './activity-selector.component.css'
})
export class ActivitySelectorComponent {
  selectedActivity?: MainActivity;

  constructor(private http: HttpClient) { }
  submit() {
    console.log('Sending activity:', this.selectedActivity);

    this.http.post('/api/activities', this.selectedActivity).subscribe(() => {
      console.log('Submitted running activity!');
    });
  }

  // Activity Types For Creator
  get runningActivity(): Running {
    return (this.selectedActivity as Running);
  }

  get boulderingActivity(): Bouldering {
    return (this.selectedActivity as Bouldering);
  }

  SportTypes: MainActivity[] = [
    new MainActivity(),
    new Running(),
    new Bouldering()
  ];
}
