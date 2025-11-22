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

interface RouteSegment {
  coords: RouteCoordinateDto[];
  isOutAndBack: boolean;
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

  lat: number = 47.53277;
  lng: number = 19.052245;

  snappedPoints: { lat: number; lon: number }[] = [];

  fullRouteCoords: RouteCoordinateDto[] = [];
  routeSegments: RouteSegment[] = [];

  distanceProfile: number[] = [];
  elevationProfile: number[] = [];
  totalDistanceMeters = 0;
  totalAscentMeters = 0;
  totalDescentMeters = 0;

  hasElevationProfile = false;
  elevationChartData: ChartData<'line'> = { labels: [], datasets: [] };
  elevationChartOptions: ChartOptions<'line'> = {
    responsive: true,
    elements: {
      line: {
        borderWidth: 2,
        borderColor: 'rgb(0, 200, 80)'
      },
      point: {
        radius: 0
      }
    },
    plugins: {
      legend: { display: false }
    }
  };

  private readonly apiBase = (environment as any).apiUrl ?? '';
  private readonly routeSourceId = 'route';
  private readonly pointsSourceId = 'route-points';
  private readonly pointsLayerId = 'route-points-layer';

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
      this.addEmptyPointsSourceAndLayer();
    });

    // click on map background → add new snapped point + route section
    this.map.on('click', (e) => this.onMapClick(e));

    // interactions with node circles
    this.map.on('click', this.pointsLayerId, (e) => {
      (e.originalEvent as MouseEvent).stopPropagation();

      const feature = (e.features && e.features[0]) as any;
      const index = feature?.properties?.index;
      if (index !== undefined) {
        console.log('Clicked node index:', index, 'coords:', this.snappedPoints[index]);
      }
    });

    this.map.on('mouseenter', this.pointsLayerId, () => {
      this.map.getCanvas().style.cursor = 'pointer';
    });

    this.map.on('mouseleave', this.pointsLayerId, () => {
      this.map.getCanvas().style.cursor = 'crosshair';
    });
  }

  private onMapClick(e: mapboxgl.MapMouseEvent): void {
    const { lng, lat } = e.lngLat;

    // temp local point (will be replaced by snapped coords)
    this.snappedPoints.push({ lat, lon: lng });

    const params = new HttpParams()
      .set('lat', lat.toString())
      .set('lon', lng.toString());

    const url = `${this.apiBase}/api/routing/snap`;

    this.http.get<SnapResponseDto>(url, { params }).subscribe({
      next: (snap) => {
        console.log(snap);

        // update last snapped point to the server-snapped position
        this.snappedPoints[this.snappedPoints.length - 1] = {
          lat: snap.lat,
          lon: snap.lon
        };
        this.updatePointsSource();

        if (this.snappedPoints.length >= 2) {
          const count = this.snappedPoints.length;
          const from = this.snappedPoints[count - 2];
          const to = this.snappedPoints[count - 1];
          this.requestRoute(from, to);
        }
      },
      error: (err) => {
        console.error('[Snap] failed', err);
        alert('Could not snap to road.');
        this.snappedPoints.pop();
        this.updatePointsSource();
      }
    });
  }

  private addEmptyRouteSource(): void {
    const emptyData: GeoJSON.FeatureCollection<GeoJSON.Geometry> = {
      type: 'FeatureCollection',
      features: []
    };

    this.map.addSource(this.routeSourceId, {
      type: 'geojson',
      data: emptyData
    });

    this.map.addLayer({
      id: 'route-line',
      type: 'line',
      source: this.routeSourceId,
      layout: {
        'line-join': 'round',
        'line-cap': 'round'
      },
      paint: {
        'line-width': 4,
        'line-color': '#00c850'
      }
    });
  }

  private addEmptyPointsSourceAndLayer(): void {
    const empty: GeoJSON.FeatureCollection<GeoJSON.Geometry> = {
      type: 'FeatureCollection',
      features: []
    };

    this.map.addSource(this.pointsSourceId, {
      type: 'geojson',
      data: empty
    });

    this.map.addLayer({
      id: this.pointsLayerId,
      type: 'circle',
      source: this.pointsSourceId,
      paint: {
        'circle-radius': 5,
        'circle-color': '#00c850',
        'circle-stroke-width': 2,
        'circle-stroke-color': '#000000'
      }
    });

    // default cursor on the canvas
    this.map.getCanvas().style.cursor = 'crosshair';
  }

  private updatePointsSource(): void {
    const src = this.map.getSource(this.pointsSourceId) as mapboxgl.GeoJSONSource | undefined;
    if (!src) return;

    const features: GeoJSON.Feature<GeoJSON.Point>[] = this.snappedPoints.map((p, idx) => ({
      type: 'Feature',
      properties: { index: idx },
      geometry: {
        type: 'Point',
        coordinates: [p.lon, p.lat]
      }
    }));

    const fc: GeoJSON.FeatureCollection<GeoJSON.Point> = {
      type: 'FeatureCollection',
      features
    };

    src.setData(fc);
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

    this.http.post<RouteCoordinateDto[]>(url, body).subscribe({
      next: (coords) => {
        if (!coords || coords.length === 0) {
          console.warn('Route returned no coordinates');
          return;
        }

        let segment = coords;
        if (this.routeSegments.length > 0) {
          segment = coords.slice(1);
        }

        this.routeSegments.push({
          coords: segment,
          isOutAndBack: false
        });
        this.rebuildRouteFromSegments();
      },
      error: (err) => {
        console.error('Route request failed', err);
        alert('Could not calculate route.');
      }
    });
  }

  private rebuildRouteFromSegments(): void {
    if (this.routeSegments.length === 0) {
      this.fullRouteCoords = [];
      this.updateRouteSourceEmpty();
      this.clearProfiles();
      return;
    }

    this.fullRouteCoords = this.routeSegments.flatMap(s => s.coords);
    this.updateRouteOnMap();
    this.recomputeDistanceProfile();
    this.fetchElevationProfile();
  }


  private updateRouteSourceEmpty(): void {
    const src = this.map.getSource(this.routeSourceId) as mapboxgl.GeoJSONSource | undefined;
    if (!src) return;

    const emptyFc: GeoJSON.FeatureCollection<GeoJSON.Geometry> = {
      type: 'FeatureCollection',
      features: []
    };
    src.setData(emptyFc);
  }

  private updateRouteOnMap(): void {
    const source = this.map.getSource(this.routeSourceId) as mapboxgl.GeoJSONSource | undefined;
    if (!source || this.fullRouteCoords.length === 0) {
      this.updateRouteSourceEmpty();
      return;
    }

    const lineString: GeoJSON.Feature<GeoJSON.LineString> = {
      type: 'Feature',
      properties: {},
      geometry: {
        type: 'LineString',
        coordinates: this.fullRouteCoords.map((c) => [c.lon, c.lat])
      }
    };

    const fc: GeoJSON.FeatureCollection<GeoJSON.LineString> = {
      type: 'FeatureCollection',
      features: [lineString]
    };

    source.setData(fc);
  }

  // ---------- Distance / Elevation ----------

  private recomputeDistanceProfile(): void {
    const n = this.fullRouteCoords.length;
    this.distanceProfile = [];
    this.totalDistanceMeters = 0;

    if (n === 0) {
      this.clearProfiles();
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
      this.clearProfiles();
      return;
    }

    const url = `${this.apiBase}/api/routing/elevation`;
    this.http.post<(number | null)[]>(url, this.fullRouteCoords).subscribe({
      next: (elevs) => {
        this.elevationProfile = elevs.map((e) => (e == null ? 0 : e));
        this.updateElevationChart();
      },
      error: (err) => {
        console.error('Elevation request failed', err);
        this.clearProfiles();
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
      this.clearProfiles();
      return;
    }

    const labels = this.distanceProfile.map((d) => (d / 1000).toFixed(2));

    this.elevationChartData = {
      labels,
      datasets: [
        {
          label: 'Elevation',
          data: this.elevationProfile,
          borderWidth: 2,
          fill: false,
          tension: 0.1
        }
      ]
    };

    let asc = 0;
    let desc = 0;
    for (let i = 1; i < this.elevationProfile.length; i++) {
      const delta = this.elevationProfile[i] - this.elevationProfile[i - 1];
      if (delta > 0) asc += delta;
      else desc -= delta;
    }

    this.totalAscentMeters = asc;
    this.totalDescentMeters = desc;
    this.hasElevationProfile = true;
  }

  private clearProfiles(): void {
    this.elevationProfile = [];
    this.distanceProfile = [];
    this.elevationChartData = { labels: [], datasets: [] };
    this.totalDistanceMeters = 0;
    this.totalAscentMeters = 0;
    this.totalDescentMeters = 0;
    this.hasElevationProfile = false;
  }

  private haversine(lat1: number, lon1: number, lat2: number, lon2: number): number {
    const R = 6371000;
    const toRad = (deg: number) => (deg * Math.PI) / 180;

    const dLat = toRad(lat2 - lat1);
    const dLon = toRad(lon2 - lon1);
    const a =
      Math.sin(dLat / 2) * Math.sin(dLat / 2) +
      Math.cos(toRad(lat1)) *
      Math.cos(toRad(lat2)) *
      Math.sin(dLon / 2) *
      Math.sin(dLon / 2);

    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    return R * c;
  }

  // ---------- Toolbar actions ----------

  undoLastSection(): void {
    // Case 1: if we have route segments then undo the last segment
    if (this.routeSegments.length > 0) {
      const last = this.routeSegments.pop();
      if (!last) {
        return;
      }

      // Only remove a snapped point if this segment was created by a user click (not by Out & Back)
      if (!last.isOutAndBack && this.snappedPoints.length > 1) {
        this.snappedPoints.pop();
        this.updatePointsSource();
      }

      this.rebuildRouteFromSegments();
      return;
    }

    // Case 2: no segments yet, but at least one snapped point then remove the last point
    if (this.snappedPoints.length > 0) {
      this.snappedPoints.pop();
      this.updatePointsSource();

      // No actual route line anymore
      this.fullRouteCoords = [];
      this.updateRouteSourceEmpty();
      this.clearProfiles(); // sets distance/ascent/descent to 0, hasElevationProfile = false
      return;
    }

  }




  addOutAndBack(): void {
    if (this.fullRouteCoords.length < 2) return;

    const reversed = [...this.fullRouteCoords].reverse();
    const segmentBack = reversed.slice(1);

    this.routeSegments.push({
      coords: segmentBack,
      isOutAndBack: true
    });

    this.rebuildRouteFromSegments();
  }


  saveRoute(): void {
    console.log('Save route clicked. Implement later.');
  }

  downloadGpx(): void {
    if (this.totalDistanceMeters === 0 || this.fullRouteCoords.length === 0) {
      return;
    }

    const parts: string[] = [];
    parts.push('<?xml version="1.0" encoding="UTF-8"?>');
    parts.push(
      '<gpx version="1.1" creator="APUS" xmlns="http://www.topografix.com/GPX/1/1">'
    );
    parts.push('<trk><name>Planned route</name><trkseg>');

    for (let i = 0; i < this.fullRouteCoords.length; i++) {
      const p = this.fullRouteCoords[i];
      const ele =
        this.elevationProfile && this.elevationProfile.length > i
          ? this.elevationProfile[i]
          : null;

      parts.push(
        `<trkpt lat="${p.lat.toFixed(7)}" lon="${p.lon.toFixed(7)}">`
      );
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

  private clearAll(): void {
    this.snappedPoints = [];
    this.routeSegments = [];
    this.fullRouteCoords = [];
    this.updateRouteSourceEmpty();
    this.updatePointsSource();
    this.clearProfiles();
  }
}
