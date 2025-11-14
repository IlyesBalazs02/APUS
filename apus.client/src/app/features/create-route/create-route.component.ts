import { AfterViewInit, Component, OnDestroy } from '@angular/core';
import * as mapboxgl from 'mapbox-gl';
import { environment } from '../../../environments/environment';
import { HttpClient, HttpParams } from '@angular/common/http';
import { ChartData, ChartOptions } from 'chart.js';

interface SnapResponseDto {
  nodeId: number;
  lat: number;
  lon: number;
}

interface RouteCoordinateDto {
  lat: number;
  lon: number;
}

interface RouteRequestDto {
  fromLat: number;
  fromLon: number;
  toLat: number;
  toLon: number;
}

@Component({
  selector: 'app-create-route',
  standalone: false,
  templateUrl: './create-route.component.html',
  styleUrls: ['./create-route.component.css']
})
export class CreateRouteComponent implements AfterViewInit, OnDestroy {
  map!: mapboxgl.Map;
  style = 'mapbox://styles/mapbox/streets-v11';

  // Initial center
  lat: number = 47.53277;
  lng: number = 19.052245;

  private markers: mapboxgl.Marker[] = [];
  private snappedPoints: { lat: number; lon: number }[] = [];
  fullRouteCoords: RouteCoordinateDto[] = [];

  // distance / elevation
  distanceProfile: number[] = [];   // meters from start
  elevationProfile: number[] = [];  // meters
  totalDistanceMeters = 0;

  // chart
  hasElevationProfile = false;
  elevationChartData: ChartData<'line'> = { labels: [], datasets: [] };
  elevationChartOptions: ChartOptions<'line'> = {
    responsive: true,
    elements: {
      line: {
        borderWidth: 3,
        borderColor: 'rgb(75, 192, 192)',
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

  private readonly apiBase = (environment as any).apiUrl ?? '';

  constructor(private http: HttpClient) { }

  ngAfterViewInit(): void {
    this.initMap();
  }

  ngOnDestroy(): void {
    if (this.map) {
      this.map.remove();
    }
  }

  // ---------- Map init & events ----------

  private initMap(): void {
    this.map = new mapboxgl.Map({
      accessToken: environment.mapbox.accessToken,
      container: 'map',
      style: this.style,
      zoom: 13,
      center: [this.lng, this.lat]
    });

    this.map.addControl(new mapboxgl.NavigationControl());

    this.map.on('load', () => {
      this.addEmptyRouteSource();
      this.addMarker(this.lat, this.lng);
    });

    this.map.on('click', (e) => this.onMapClick(e));
  }

  private onMapClick(e: mapboxgl.MapMouseEvent): void {
    const { lng, lat } = e.lngLat;

    this.snappedPoints.push({ lat, lon: lng });

    const params = new HttpParams()
      .set('lat', lat.toString())
      .set('lon', lng.toString());

    const url = `${this.apiBase}/api/routing/snap`;

    this.http.get<SnapResponseDto>(url, { params })
      .subscribe({
        next: snap => {
          this.addMarker(snap.lat, snap.lon);

          this.snappedPoints[this.snappedPoints.length - 1] = {
            lat: snap.lat,
            lon: snap.lon
          };

          if (this.snappedPoints.length >= 2) {
            const count = this.snappedPoints.length;
            const from = this.snappedPoints[count - 2];
            const to = this.snappedPoints[count - 1];

            this.requestRoute(from, to);
          }
        },
        error: err => {
          console.error('[Snap] failed', err);
          alert('Could not snap to road. Check Network tab for /api/routing/snap.');
        }
      });
  }

  private addEmptyRouteSource(): void {
    const emptyData: GeoJSON.FeatureCollection<GeoJSON.Geometry> = {
      type: 'FeatureCollection',
      features: []
    };

    this.map.addSource('route', {
      type: 'geojson',
      data: emptyData
    });

    this.map.addLayer({
      id: 'route-line',
      type: 'line',
      source: 'route',
      layout: {
        'line-join': 'round',
        'line-cap': 'round'
      },
      paint: {
        'line-width': 4,
        'line-color': '#ff0000'
      }
    });
  }

  private addMarker(lat: number, lon: number): void {
    const marker = new mapboxgl.Marker()
      .setLngLat([lon, lat])
      .addTo(this.map);

    this.markers.push(marker);
  }

  // ---------- Routing ----------

  private requestRoute(
    from: { lat: number; lon: number },
    to: { lat: number; lon: number }
  ): void {
    const body: RouteRequestDto = {
      fromLat: from.lat,
      fromLon: from.lon,
      toLat: to.lat,
      toLon: to.lon
    };

    const url = `${this.apiBase}/api/routing/route`;

    this.http.post<RouteCoordinateDto[]>(url, body)
      .subscribe({
        next: (coords) => {
          if (!coords || coords.length === 0) {
            console.warn('Route returned no coordinates');
            return;
          }

          if (this.fullRouteCoords.length === 0) {
            this.fullRouteCoords = coords;
          } else {
            const toAppend = coords.slice(1); // avoid duplicate joint
            this.fullRouteCoords = this.fullRouteCoords.concat(toAppend);
          }

          this.updateRouteOnMap();
          this.recomputeDistanceProfile();
          this.fetchElevationProfile();
        },
        error: err => {
          console.error('Route request failed', err);
          alert('Could not calculate route (see console for details).');
        }
      });
  }

  private updateRouteOnMap(): void {
    const lineString: GeoJSON.Feature<GeoJSON.LineString> = {
      type: 'Feature',
      properties: {},
      geometry: {
        type: 'LineString',
        coordinates: this.fullRouteCoords.map(c => [c.lon, c.lat])
      }
    };

    const fc: GeoJSON.FeatureCollection<GeoJSON.LineString> = {
      type: 'FeatureCollection',
      features: [lineString]
    };

    const source = this.map.getSource('route') as mapboxgl.GeoJSONSource;
    source.setData(fc);
  }

  // ---------- Distance / Elevation ----------

  private recomputeDistanceProfile(): void {
    const n = this.fullRouteCoords.length;
    this.distanceProfile = [];
    this.totalDistanceMeters = 0;

    if (n === 0) {
      this.hasElevationProfile = false;
      return;
    }

    this.distanceProfile = new Array(n).fill(0);
    for (let i = 1; i < n; i++) {
      const prev = this.fullRouteCoords[i - 1];
      const curr = this.fullRouteCoords[i];
      const d = this.haversine(prev.lat, prev.lon, curr.lat, curr.lon);
      this.totalDistanceMeters += d;
      this.distanceProfile[i] = this.totalDistanceMeters;
    }
  }

  private fetchElevationProfile(): void {
    const n = this.fullRouteCoords.length;
    if (n === 0) {
      this.elevationProfile = [];
      this.hasElevationProfile = false;
      this.elevationChartData = { labels: [], datasets: [] };
      return;
    }

    const url = `${this.apiBase}/api/routing/elevation`;
    this.http.post<(number | null)[]>(url, this.fullRouteCoords)
      .subscribe({
        next: (elevs) => {
          this.elevationProfile = elevs.map(e => (e == null ? 0 : e));
          this.updateElevationChart();
        },
        error: err => {
          console.error('Elevation request failed', err);
          this.elevationProfile = [];
          this.hasElevationProfile = false;
          this.elevationChartData = { labels: [], datasets: [] };
        }
      });
  }

  private updateElevationChart(): void {
    const n = this.fullRouteCoords.length;
    if (
      n === 0 ||
      this.distanceProfile.length !== n ||
      this.elevationProfile.length !== n
    ) {
      this.hasElevationProfile = false;
      this.elevationChartData = { labels: [], datasets: [] };
      return;
    }

    // labels = distance in km
    const labels = this.distanceProfile.map(d => (d / 1000).toFixed(2));

    this.elevationChartData = {
      labels,
      datasets: [
        {
          label: 'Elevation',
          data: this.elevationProfile,
          borderWidth: 3,
          fill: false,
          tension: 0.1
        }
      ]
    };

    this.hasElevationProfile = true;
  }

  private haversine(lat1: number, lon1: number, lat2: number, lon2: number): number {
    const R = 6371000; // meters
    const toRad = (deg: number) => deg * Math.PI / 180;

    const dLat = toRad(lat2 - lat1);
    const dLon = toRad(lon2 - lon1);
    const a =
      Math.sin(dLat / 2) * Math.sin(dLat / 2) +
      Math.cos(toRad(lat1)) * Math.cos(toRad(lat2)) *
      Math.sin(dLon / 2) * Math.sin(dLon / 2);

    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    return R * c;
  }

  // ---------- GPX download ----------

  downloadGpx(): void {
    if (this.totalDistanceMeters === 0 || this.fullRouteCoords.length === 0) {
      return;
    }

    const parts: string[] = [];
    parts.push('<?xml version="1.0" encoding="UTF-8"?>');
    parts.push('<gpx version="1.1" creator="APUS" xmlns="http://www.topografix.com/GPX/1/1">');
    parts.push('<trk><name>Planned route</name><trkseg>');

    for (let i = 0; i < this.fullRouteCoords.length; i++) {
      const p = this.fullRouteCoords[i];
      const ele = (this.elevationProfile && this.elevationProfile.length > i)
        ? this.elevationProfile[i]
        : null;

      parts.push(`<trkpt lat="${p.lat.toFixed(7)}" lon="${p.lon.toFixed(7)}">`);
      if (ele != null && !Number.isNaN(ele)) {
        parts.push(`<ele>${ele.toFixed(1)}</ele>`);
      }
      parts.push('</trkpt>');
    }

    parts.push('</trkseg></trk></gpx>');

    const blob = new Blob([parts.join('')], { type: 'application/gpx+xml' });
    const url = URL.createObjectURL(blob);

    const a = document.createElement('a');
    a.href = url;
    a.download = 'route.gpx';
    a.click();

    URL.revokeObjectURL(url);
  }
}
