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
        dto => this.activity = createActivity(dto),
        err => console.error(err)
      );
  }

  // map each activityType to the array of fields to show
  fieldConfig: Record<string, string[]> = {
    MainActivity: ['title', 'description', 'date', 'duration'],
    Running: ['title', 'date', 'duration', 'totaldistancekm', 'avgpace'],
    // …etc
  };

  labels: Record<string, string> = {
    title: 'Title',
    date: 'Date',
    duration: 'Duration',
    totaldistancekm: 'Distance (km)',
    avgpace: 'Avg. Pace',
    difficulty: 'Difficulty',
    // …
  };

  get fieldsToShow(): string[] {
    // fall back to the base activity if no match
    return this.fieldConfig[this.activity.activityType] ||
      this.fieldConfig['MainActivity'];
  }
}
