import { Component, OnInit } from '@angular/core';
import { MainActivity, Running, Bouldering } from '../../_models/ActivityClasses';
import { HttpClient } from '@angular/common/http';


@Component({
  selector: 'app-list-activities',
  standalone: false,
  templateUrl: './list-activities.component.html',
  styleUrl: './list-activities.component.css'
})
export class ListActivitiesComponent {
  public activities: MainActivity[] = [];

  constructor(private http: HttpClient) { }

  baseKeys: string[] = Object.keys(new MainActivity());

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

  isUniqueProperty(key: string): boolean {
    return !this.baseKeys.includes(key) && key !== '$type'; // exclude $type too
  }

  capitalizeFirstLetter(key: string): string {
    if (!key) return '';
    return key.charAt(0).toUpperCase() + key.slice(1);
  }

  /*
  getForecasts() {
    this.http.get<WeatherForecast[]>('/weatherforecast').subscribe(
      (result) => {
        this.forecasts = result;
      },
      (error) => {
        console.error(error);
      }
    );
  }*/
}
