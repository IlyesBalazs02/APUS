import { Component, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { createActivity, MainActivity } from '../../_models/ActivityClasses';
import { HttpClient } from '@angular/common/http';
import { catchError, forkJoin, of, throwError, Timestamp } from 'rxjs';
import { Trackpoint } from '../../ActivityDto/TrackpointDto';
import { ChartData, ChartOptions, ChartType } from 'chart.js';
import { Router } from '@angular/router';

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

  constructor(private route: ActivatedRoute, private http: HttpClient, private router: Router) {
    this.activityId = this.route.snapshot.paramMap.get('id')!;
  }

  ngOnInit() {
    const activity$ = this.http.get<MainActivity>(`/api/activities/${this.activityId}`);
    const images$ = this.http.get<string[]>(`/api/images/${this.activityId}`);

    const trackpoints$ = this.http
      .get<Trackpoint[]>(`/api/activityfile/${this.activityId}`)
      .pipe(
        catchError(err => {
          // ANY error (404, 500, etc.) → just behave as "no track"
          console.warn('Track loading failed, continuing without track', err);
          return of<Trackpoint[]>([]);
        })
      );

    forkJoin({ activity: activity$, images: images$, trackpoints: trackpoints$ })
      .subscribe({
        next: ({ activity, images, trackpoints }) => {
          this.activity = createActivity(activity);
          this.images = images;
          this.trackpoints = trackpoints ?? []; // safe default
        },
        // you can even drop the error handler here if everything is caught above
        error: err => console.error('Unexpected error in display-activity', err)
      });
  }


  ngOnChanges(changes: SimpleChanges): void {
  }

  // map each activityType to the array of fields to show
  fieldConfig: Record<string, string[]> = {
    MainActivity: ['avgHr', 'totalCalories'],
    GpsRelatedActivity: ['distanceKm', 'elevationGain'],
    Running: ['distanceKm', 'pace', 'elevationGain'],
  };

  labels: Record<string, string> = {
    title: 'Title',
    date: 'Date',
    duration: 'Time',

    distanceKm: 'Distance (km)',
    pace: 'Avg. Pace',
    difficulty: 'Difficulty',
    elevationGain: 'Elevation gain',

    avgHr: 'Avg. Heartrate',
    totalCalories: 'Calories',
  };


  get fieldsToShow(): string[] {
    const mainFields = this.fieldConfig['MainActivity'];
    const activityFields = this.fieldConfig[this.activity.activityType] || [];

    const allFields = Array.from(new Set([...mainFields, ...activityFields]))
      .filter(f => (this.activity as any)[f] != null);

    // duration is a must-to-display element
    allFields.unshift('duration');

    return allFields;
  }


  editActivity() {
    this.router.navigate([`/activities/${this.activityId}/edit`]);
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

