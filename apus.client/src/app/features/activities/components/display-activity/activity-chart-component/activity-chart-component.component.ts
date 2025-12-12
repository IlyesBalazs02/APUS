import { Component, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { Trackpoint } from '../../../ActivityDto/TrackpointDto';
import { ChartData, ChartOptions } from 'chart.js';

@Component({
  selector: 'app-activity-chart-component',
  standalone: false,
  templateUrl: './activity-chart-component.component.html',
  styleUrl: './activity-chart-component.component.css'
})

export class ActivityChartComponentComponent implements OnChanges {
  @Input() trackpoints: Trackpoint[] = [];

  //if it has coordinates, show the elevation profile
  hasCoordinates: boolean = false;

  hasHeartRate: boolean = false;

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['trackpoints']) return;

    this.hasCoordinates = this.trackpoints.some(tp => tp.lon != null);
    if (this.hasCoordinates) this.buildElevationChart();

    this.hasHeartRate = this.trackpoints.some(tp => tp.hr != null);
    if (this.hasHeartRate) this.buildHrChart();
  }

  private buildElevationChart(): void {

    const pts = this.trackpoints
      .filter(p => p.lat != null && p.lon != null && p.alt != null)
      .map(p => ({ lat: p.lat!, lon: p.lon!, elevation: p.alt! }));

    //check if there is enough coordinates
    if (pts.length < 2) {
      console.log('DisplayActivityComponent: filtered points', pts);
      this.elevationChartData = { labels: [], datasets: [] };
      this.hasCoordinates = false;
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
    const pts = this.trackpoints
      .filter(p => p.hr != null && p.time != null)
      .map(p => ({ heartrate: p.hr, time: p.time }));

    if (pts.length < 2) {
      this.hrChartData = { labels: [], datasets: [] };
      this.hasHeartRate = false;
      return;
    }

    const timestamps = pts.map(p => new Date(p.time).getTime());

    const MAX_GAP_MS = 2000;

    const elapsedMs: number[] = [];
    let elapsedTotal = 0;
    for (let i = 0; i < timestamps.length; i++) {
      if (i === 0) {
        elapsedMs.push(0);
      } else {
        const delta = timestamps[i] - timestamps[i - 1];
        if (delta <= MAX_GAP_MS) {
          elapsedTotal += delta;
        }
        elapsedMs.push(elapsedTotal);
      }
    }

    const elapsedSecs = elapsedMs.map(ms => Math.floor(ms / 1000));

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
    maintainAspectRatio: false,
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
    maintainAspectRatio: false,
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
    const R = 6371;
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
