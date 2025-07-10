import { Component, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { createActivity, MainActivity } from '../../_models/ActivityClasses';
import { HttpClient } from '@angular/common/http';
import { forkJoin, Timestamp } from 'rxjs';
import { Trackpoint } from '../../ActivityDto/TrackpointDto';
import { ChartData, ChartOptions, ChartType } from 'chart.js';

@Component({
  selector: 'app-display-activity',
  standalone: false,
  templateUrl: './display-activity.component.html',
  styleUrls: ['./display-activity.component.scss']
})



export class DisplayActivityComponent implements OnInit, OnChanges {
  activityId: string;
  activity: MainActivity = new MainActivity();

  images: string[] = [];
  selectedIndex: number | null = null;

  trackpoints: Trackpoint[] = [];

  constructor(private route: ActivatedRoute, private http: HttpClient) {
    this.activityId = this.route.snapshot.paramMap.get('id')!;
  }

  ngOnInit() {
    const activity$ = this.http.get<MainActivity>(`/api/activities/${this.activityId}`);
    const images$ = this.http.get<string[]>(`/api/images/${this.activityId}`);
    const trackpointdto$ = this.http.get<Trackpoint[]>(`/api/activityfile/${this.activityId}`);

    forkJoin({ activity: activity$, images: images$, trackpoints: trackpointdto$ })
      .subscribe({
        next: ({ activity: dto, images, trackpoints }) => {
          this.activity = createActivity(dto);
          this.images = images;
          this.trackpoints = trackpoints;
        },
        error: err => console.error(err)
      });
  }

  ngOnChanges(changes: SimpleChanges): void {
  }

  // map each activityType to the array of fields to show
  fieldConfig: Record<string, string[]> = {
    MainActivity: ['avgHeartRate', 'calories'],
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
    totalAscentMeters: 'Elevation gain',
    avgHeartRate: 'Avg. Heartrate',
    calories: 'Calories'
    // …
  };

  get fieldsToShow(): string[] {
    const mainFields = this.fieldConfig['MainActivity'];
    const activityFields = this.fieldConfig[this.activity.activityType] || [];

    // only keep fields where activity[field] is non-null/undefined (and you can add
    const allFields = Array.from(new Set([...mainFields, ...activityFields])).filter(f => this.activity[f] != null);

    //duration is a must-to-display element
    allFields.unshift('duration');

    return allFields;
  }



  openViewer(i: number) {
    this.selectedIndex = i;
  }

  prevImage(event: MouseEvent) {
    event.stopPropagation();
    if (this.selectedIndex === null) return;
    const len = this.images.length;
    this.selectedIndex = (this.selectedIndex + len - 1) % len;
  }

  nextImage(event: MouseEvent) {
    event.stopPropagation();
    if (this.selectedIndex === null) return;
    const len = this.images.length;
    this.selectedIndex = (this.selectedIndex + 1) % len;
  }

  closeViewer() {
    this.selectedIndex = null;
  }

}

