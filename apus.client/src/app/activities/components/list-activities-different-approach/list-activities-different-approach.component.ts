import { Component } from '@angular/core';
import { MainActivity, Running, Bouldering } from '../../_models/ActivityClasses';
import { HttpClient } from '@angular/common/http';
import { RunningActivityComponent } from './ActivityCards/running-activity/running-activity.component';
import { CommonModule } from '@angular/common';
import { Injector } from '@angular/core';

@Component({
  selector: 'app-list-activities-different-approach',
  standalone: false,
  templateUrl: './list-activities-different-approach.component.html',
  styleUrl: './list-activities-different-approach.component.css'
})
export class ListActivitiesDifferentApproachComponent {
  public activities: MainActivity[] = [];

  constructor(private http: HttpClient, private injector: Injector) { }

  ngOnInit() {
    this.getActivities();
  }

  getActivities() {
    this.http.get<MainActivity[]>('/api/activities').subscribe(
      (result) => {
        this.activities = result.map(activity => {
          switch (activity.$type) {
            case 'APUS.Server.Models.Activities.Running, APUS.Server':
              return Object.assign(new Running(), activity);
            case 'APUS.Server.Models.Activities.Bouldering, APUS.Server':
              return Object.assign(new Bouldering(), activity);
            default:
              return Object.assign(new MainActivity(), activity);
          }
        });
      },
      (error) => {
        console.error(error);
      }
    );
  }

  createInjector(activity: MainActivity): Injector {
    return Injector.create({
      providers: [{ provide: Running, useValue: activity }],
      parent: this.injector
    });
  }

  getComponent(type: string) {
    console.log('activities', type); // Check if activities are populated

    switch (type) {
      case 'Running': return RunningActivityComponent;
      default: return null;
    }
  }
}
