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

  //Elev/distabce
  elevPoints: Trackpoint[] = [];
  hasCoordinates: boolean = false;

  //Hr/Time
  hrpoints: Trackpoint[] = [];
  hasHrAndTime: boolean = false;

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
              this.elevPoints = resp;
              this.hasCoordinates = this.elevPoints.some(tp => tp.lon != null);


              this.hrpoints = resp;
              this.hasHrAndTime = this.hrpoints.some(hp => hp.hr != null && hp.time);

              console.log(this.hrpoints);

              this.buildElevationChart();
              this.buildHrChart();
            });

        },
        error: err => console.error(err)
      });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['elevPoints']) this.buildElevationChart();

    if (changes['hrpoints']) this.buildHrChart();
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



  private buildElevationChart(): void {

    // Filter & map alt to elevation
    const pts = this.elevPoints
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

  private buildHrChart(): void {
    const pts = this.hrpoints
      .filter(p => p.hr != null && p.time != null)
      .map(p => ({ heartrate: p.hr, time: p.time }));

    // 1) turn ISO strings into millisecond timestamps
    const timestamps = pts.map(p => new Date(p.time).getTime());

    // max time gap between trackpoints, to filter out the time between pauses
    const MAX_GAP_MS = 2000;

    const elapsedMs: number[] = [];
    let elapsedTotal = 0;
    for (let i = 0; i < timestamps.length; i++) {
      if (i === 0) {
        elapsedMs.push(0);
      } else {
        const delta = timestamps[i] - timestamps[i - 1];
        // only accept if its lower than the max_gap
        if (delta <= MAX_GAP_MS) {
          elapsedTotal += delta;
        }
        elapsedMs.push(elapsedTotal);
      }
    }

    // 4) convert to whole seconds
    const elapsedSecs = elapsedMs.map(ms => Math.floor(ms / 1000));

    // 5) format hh:mm:ss
    function formatHMS(totalSecs: number): string {
      const h = Math.floor(totalSecs / 3600);
      const m = Math.floor((totalSecs % 3600) / 60);
      const s = totalSecs % 60;
      return [h, m, s]
        .map(n => String(n).padStart(2, '0'))
        .join(':');
    }


    this.hrChartData = {
      labels: elapsedSecs.map(formatHMS),
      datasets: [{
        data: pts.map(p => p.heartrate!),
        label: 'HeartRate',
        fill: false,
        tension: 0.1,
        borderColor: 'rgb(228, 7, 7)',
        borderWidth: 3
      }]
    };

  }

  public elevationChartType: 'line' = 'line';

  //Elevation/Distance
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

  //Hr/Time
  public hrChartData: ChartData<'line'> = { labels: [], datasets: [] };
  public hrChartOptions: ChartOptions<'line'> = {
    responsive: true,
    elements: {
      line: { borderWidth: 3, borderColor: 'rgb(211, 30, 6)' },
      point: { radius: 0 }
    },
    scales: {
      x: { title: { display: true, text: 'Time' } },

      y: { title: { display: true, text: 'Heart Rate (bpm)' } }
    }
  };

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

