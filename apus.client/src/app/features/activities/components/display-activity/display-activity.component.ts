import { Component, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { createActivity, MainActivity } from '../../_models/ActivityClasses';
import { HttpClient } from '@angular/common/http';
import { catchError, forkJoin, of, throwError, Timestamp } from 'rxjs';
import { Trackpoint } from '../../ActivityDto/TrackpointDto';
import { ActivityImageDto } from '../../ActivityDto/ActivityImageDto';
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

  images: ActivityImageDto[] = [];
  selectedIndex: number | null = null;

  trackpoints: Trackpoint[] = [];

  constructor(private route: ActivatedRoute, private http: HttpClient, private router: Router) {
    this.activityId = this.route.snapshot.paramMap.get('id')!;
  }

  ngOnInit() {
    const activity$ = this.http.get<MainActivity>(`/api/activities/${this.activityId}`);
    const images$ = this.http.get<ActivityImageDto[]>(`/api/images/${this.activityId}`);

    const trackpoints$ = this.http
      .get<Trackpoint[]>(`/api/activityfile/${this.activityId}`)
      .pipe(
        catchError(err => {
          console.warn('Track loading failed, continuing without track', err);
          return of<Trackpoint[]>([]);
        })
      );

    forkJoin({ activity: activity$, images: images$, trackpoints: trackpoints$ })
      .subscribe({
        next: ({ activity, images, trackpoints }) => {
          this.activity = createActivity(activity);
          console.log('activityType =', this.activity.activityType, this.activity);
          this.images = images;
          this.trackpoints = trackpoints ?? [];
        },
        error: err => console.error('Unexpected error in display-activity', err)
      });
  }


  ngOnChanges(changes: SimpleChanges): void {
  }

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

  get imageMapData(): { lat: number; lon: number; url: string }[] {
    if (!this.trackpoints?.length) {
      return [];
    }

    // Collect valid trackpoint times
    const trackTimes: Date[] = this.trackpoints
      .map(tp => {
        const t = (tp as any).time as string | null | undefined;
        if (!t) return null;
        const d = new Date(t);
        return isNaN(d.getTime()) ? null : d;
      })
      .filter((d): d is Date => d !== null);

    if (!trackTimes.length) {

      return [];
    }

    const minTimeMs = Math.min(...trackTimes.map(d => d.getTime()));
    const maxTimeMs = Math.max(...trackTimes.map(d => d.getTime()));

    const fiveHoursMs = 5 * 60 * 60 * 1000;

    const lowerBound = minTimeMs - fiveHoursMs;
    const upperBound = maxTimeMs + fiveHoursMs;

    // Keep only images whose time is within the track time and have coordinates
    return this.images
      .filter(img => img.lat != null && img.lon != null && img.dateTaken)
      .filter(img => {
        const d = new Date(img.dateTaken!);
        if (isNaN(d.getTime())) return false;
        const t = d.getTime();
        return t >= lowerBound && t <= upperBound;
      })
      .map(img => ({
        lat: img.lat as number,
        lon: img.lon as number,
        url: img.url
      }));
  }

  public formatPace(value: any): string {
    const speed = typeof value === 'number' ? value : Number(value);

    if (!isFinite(speed) || speed <= 0) {
      return '-';
    }

    const secondsPerKm = 1000 / speed; // m/s -> s/km
    const minutes = Math.floor(secondsPerKm / 60);
    const seconds = Math.round(secondsPerKm % 60);
    const secStr = seconds.toString().padStart(2, '0');

    return `${minutes}:${secStr}`;
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

