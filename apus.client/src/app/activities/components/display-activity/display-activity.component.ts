import { Component, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { createActivity, MainActivity } from '../../_models/ActivityClasses';
import { HttpClient } from '@angular/common/http';
import { forkJoin } from 'rxjs';
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
  hasCoordinates: boolean = false;

  constructor(private route: ActivatedRoute, private http: HttpClient) {
    this.activityId = this.route.snapshot.paramMap.get('id')!;
  }

  ngOnInit() {
    const activity$ = this.http.get<MainActivity>(`/api/activities/${this.activityId}`);
    const images$ = this.http.get<string[]>(`/api/images/${this.activityId}`);
    const trackpointdto$ = this.http.get<Trackpoint[]>(`/api/activityfile/${this.activityId}`);

    forkJoin({ activity: activity$, images: images$ })
      .subscribe({
        next: ({ activity: dto, images }) => {
          this.activity = createActivity(dto);
          this.images = images;

          this.http.get<Trackpoint[]>(`/api/activityfile/${this.activityId}`)
            .subscribe(resp => {
              this.trackpoints = resp;
              this.hasCoordinates = this.trackpoints.some(tp => tp.lon != null);
              console.log('Loaded points:', this.trackpoints.length);

              this.buildChart();
            });

        },
        error: err => console.error(err)
      });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['trackpoints']) {
      this.buildChart();
    }
  }

  private buildChart(): void {
    // Filter & map alt to elevation
    const pts = this.trackpoints
      .filter(p => p.lat != null && p.lon != null && p.alt != null)
      .map(p => ({ lat: p.lat!, lon: p.lon!, elevation: p.alt! }));

    if (pts.length < 2) {
      console.log('DisplayActivityComponent: filtered points', pts);
      this.elevationChartData = { labels: [], datasets: [] };
      return;
    }

    // Compute cumulative distance
    const distances: number[] = [0];
    for (let i = 1; i < pts.length; i++) {
      distances.push(distances[i - 1] + this.haversine(pts[i - 1], pts[i]));
    }

    // Build chart data
    this.elevationChartData = {
      labels: distances.map(d => d.toFixed(2)),
      datasets: [{
        data: pts.map(p => p.elevation),
        label: 'Elevation (m)',
        fill: false,
        tension: 0.1,
        borderColor: 'rgb(201, 198, 37)',
        borderWidth: 3
      }]
    };
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
    //console.log(this.activity);
    const allFields = Array.from(new Set([...mainFields, ...activityFields])).filter(f => this.activity[f] != null);

    // only keep fields where activity[field] is non-null/undefined (and you can add
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


  public elevationChartData: ChartData<'line'> = { labels: [], datasets: [] };
  public elevationChartOptions: ChartOptions<'line'> = {
    responsive: true,
    elements: {
      line: {
        borderWidth: 3,
        borderColor: 'rgb(75, 192, 192)'
      },
      point: {
        radius: 0
      }
    },
    scales: {
      x: { title: { display: true, text: 'Distance (km)' } },
      y: { title: { display: true, text: 'Elevation (m)' } }
    },
    plugins: {
      legend: {
        labels: {
          usePointStyle: true,
          pointStyle: 'rect'
        }
      }
    }
  };
  public elevationChartType: 'line' = 'line';

  private toRad(deg: number): number {
    return deg * Math.PI / 180;
  }

  private haversine(
    a: { lat: number; lon: number },
    b: { lat: number; lon: number }
  ): number {
    const R = 6371; // km
    const dLat = this.toRad(b.lat - a.lat);
    const dLon = this.toRad(b.lon - a.lon);
    const lat1 = this.toRad(a.lat);
    const lat2 = this.toRad(b.lat);

    const h =
      Math.sin(dLat / 2) ** 2 +
      Math.cos(lat1) * Math.cos(lat2) * Math.sin(dLon / 2) ** 2;
    return 2 * R * Math.asin(Math.sqrt(h));
  }

}
