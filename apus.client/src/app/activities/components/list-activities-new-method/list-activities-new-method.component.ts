import { Component, OnInit } from '@angular/core';
import { BreakpointObserver } from '@angular/cdk/layout';
import * as asd from '../../_models/ActivityClasses';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-list-activities-new-method',
  standalone: false,
  templateUrl: './list-activities-new-method.component.html',
  styleUrl: './list-activities-new-method.component.css'
})
export class ListActivitiesNewMethodComponent implements OnInit {
  constructor(private http: HttpClient, public breakpointObserver: BreakpointObserver) { }
  public activities: asd.MainActivity[] = [];

  ngOnInit() {
    this.getActivities();
  }

  getActivities() {
    this.http.get<asd.MainActivity[]>('/api/activities').subscribe(
      (result) => {
        this.activities = result.map(activity => {
          switch (activity.$type) {
            case 'APUS.Server.Models.Activities.Running, APUS.Server':
              return Object.assign(new asd.Running(), activity);
            case 'APUS.Server.Models.Activities.Bouldering, APUS.Server':
              return Object.assign(new asd.Bouldering(), activity);
            default:
              return Object.assign(new asd.MainActivity(), activity);
          }
        });
      },
      (error) => {
        console.error(error);
      }
    );
  }
}
