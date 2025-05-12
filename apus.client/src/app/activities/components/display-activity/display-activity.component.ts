import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { createActivity, MainActivity } from '../../_models/ActivityClasses';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-display-activity',
  standalone: false,
  templateUrl: './display-activity.component.html',
  styleUrl: './display-activity.component.css'
})



export class DisplayActivityComponent implements OnInit {
  activityId: string;
  activity: MainActivity = new MainActivity();

  constructor(private route: ActivatedRoute, private http: HttpClient) {
    this.activityId = this.route.snapshot.paramMap.get('id')!;
  }

  ngOnInit() {
    this.http
      .get<MainActivity>(`/api/activities/${this.activityId}`)
      .subscribe(
        dto => { this.activity = createActivity(dto); /*console.log(this.activity)*/ },
        err => console.error(err)
      );
  }

  // map each activityType to the array of fields to show
  fieldConfig: Record<string, string[]> = {
    MainActivity: ['duration'],
    GpsRelatedActivity: ['totalDistanceKm', 'totalAscentMeters'],
    Running: ['totalDistanceKm', 'avgpace'],
    // …etc
  };

  labels: Record<string, string> = {
    title: 'Title',
    date: 'Date',
    duration: 'Time',
    totalDistanceKm: 'Distance (km)',
    avgpace: 'Avg. Pace',
    difficulty: 'Difficulty',
    totalAscentMeters: 'Elevation gain'
    // …
  };

  get fieldsToShow(): string[] {
    const mainFields = this.fieldConfig['MainActivity'];
    const activityFields = this.fieldConfig[this.activity.activityType] || [];
    console.log(this.activity);
    const allFields = Array.from(new Set([...mainFields, ...activityFields])).filter(f => this.activity[f] != null);

    // only keep fields where activity[field] is non-null/undefined (and you can add
    return allFields;
  }
}
