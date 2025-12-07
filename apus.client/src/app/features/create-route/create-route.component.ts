import { AfterViewInit, Component, OnDestroy, ViewEncapsulation } from '@angular/core';
import * as mapboxgl from 'mapbox-gl';
import { environment } from '../../../environments/environment';
import { HttpClient, HttpParams } from '@angular/common/http';
import { ChartData, ChartOptions } from 'chart.js';
import { UndoContext, UndoService } from './undo.service';
import { GpxImportService } from './import.service';
import { DaylightResponseDto, solarService } from './solar.service';
import { FormControl } from '@angular/forms';
import { GeocodeService, PlaceSearchResult } from './GeocodeService';
import { debounceTime, distinctUntilChanged, filter, finalize, Subscription, switchMap, tap } from 'rxjs';

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

interface SavePlannedRouteDto {
  fileName: string;
  points: RouteCoordinateDto[];
}


@Component({
  selector: 'app-create-route',
  standalone: false,
  templateUrl: './create-route.component.html',
  styleUrls: ['./create-route.component.css'],
  encapsulation: ViewEncapsulation.None
})
export class CreateRouteComponent implements AfterViewInit, OnDestroy {
  map!: mapboxgl.Map;
  style = 'mapbox://styles/mapbox/streets-v11';

  lat: number = 47.53277;
  lng: number = 19.052245;

  snappedPoints: { lat: number; lon: number }[] = [];

  // Drag state for moving existing points
  private dragPointIndex: number | null = null;
  private dragOriginalCoords: { lat: number; lon: number } | null = null;

  fullRouteCoords: RouteCoordinateDto[] = [];
  routeSegments: RouteSegment[] = [];

  distanceProfile: number[] = [];
  elevationProfile: number[] = [];
  totalDistanceMeters = 0;
  totalAscentMeters = 0;
  totalDescentMeters = 0;

  predictedSeconds: number | null = null;
  isPredicting = false;

  startDateInput: string | null = null;   // yyyy-MM-dd
  startTimeInput: string | null = null;   // HH:mm
  daylightInfo: DaylightResponseDto | null = null;

  private sunriseMarker: mapboxgl.Marker | null = null;
  private sunsetMarker: mapboxgl.Marker | null = null;

  showSaveModal = false;
  saveFileName = '';
  isSavingRoute = false;


  // --- Search state ---
  searchControl = new FormControl<string>('');
  searchResults: PlaceSearchResult[] = [];
  isSearching = false;
  showSearchResults = false;

  private searchSub?: Subscription;


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

  constructor(private http: HttpClient, public undoService: UndoService, public gpxImportService: GpxImportService, public solarService: solarService, private geocodeService: GeocodeService) { }

  ngAfterViewInit(): void {
    const now = new Date();
    this.startDateInput = now.toISOString().substring(0, 10); // yyyy-MM-dd
    this.startTimeInput = now.toTimeString().substring(0, 5); // HH:mm

    this.initMap();
    this.setupSearch();
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

    // interactions with node circles → make them draggable
    this.map.on('mousedown', this.pointsLayerId, (e: mapboxgl.MapLayerMouseEvent) => this.onPointMouseDown(e));

    this.map.on('mouseenter', this.pointsLayerId, () => {
      if (this.dragPointIndex === null) {
        this.map.getCanvas().style.cursor = 'pointer';
      }
    });

    this.map.on('mouseleave', this.pointsLayerId, () => {
      if (this.dragPointIndex === null) {
        this.map.getCanvas().style.cursor = 'crosshair';
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

  // ---------- Dragging existing points ----------

  private onPointMouseDown(e: mapboxgl.MapLayerMouseEvent): void {
    e.preventDefault();
    (e.originalEvent as MouseEvent).stopPropagation();

    const feature = (e.features && e.features[0]) as any;
    const index = feature?.properties?.index as number | undefined;
    if (index === undefined) {
      return;
    }

    this.dragPointIndex = index;
    this.dragOriginalCoords = { ...this.snappedPoints[index] };

    this.map.getCanvas().style.cursor = 'grabbing';

    this.map.on('mousemove', this.onPointDragMove);
    this.map.once('mouseup', this.onPointMouseUp);
  }

  private onPointDragMove = (e: mapboxgl.MapMouseEvent): void => {
    if (this.dragPointIndex === null) {
      return;
    }

    const { lng, lat } = e.lngLat;
    this.snappedPoints[this.dragPointIndex] = { lat, lon: lng };
    this.updatePointsSource();
  };

  private onPointMouseUp = (e: mapboxgl.MapMouseEvent): void => {
    if (this.dragPointIndex === null || !this.dragOriginalCoords) {
      this.map.getCanvas().style.cursor = 'crosshair';
      this.map.off('mousemove', this.onPointDragMove);
      return;
    }

    const idx = this.dragPointIndex;
    const previousCoords = this.dragOriginalCoords;

    this.dragPointIndex = null;
    this.dragOriginalCoords = null;

    this.map.off('mousemove', this.onPointDragMove);
    this.map.getCanvas().style.cursor = 'crosshair';

    const { lng, lat } = e.lngLat;

    const params = new HttpParams()
      .set('lat', lat.toString())
      .set('lon', lng.toString());

    const url = `${this.apiBase}/api/routing/snap`;

    this.http.get<SnapResponseDto>(url, { params }).subscribe({
      next: (snap) => {
        this.snappedPoints[idx] = { lat: snap.lat, lon: snap.lon };
        this.updatePointsSource();

        this.undoService.pushMovePoint(idx, previousCoords, { lat: snap.lat, lon: snap.lon });

        this.recalculateRouteForAllPoints();
      },
      error: (err) => {
        console.error('[Snap after drag] failed', err);
        this.snappedPoints[idx] = previousCoords;
        this.updatePointsSource();
      }
    });
  };

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

        // First point = add-point action
        if (this.snappedPoints.length === 1) {
          this.undoService.pushAddPoint();
        }

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

        // New forward segment = add-point action
        this.undoService.pushAddPoint();
      },
      error: (err) => {
        console.error('Route request failed', err);
        alert('Could not calculate route.');
      }
    });
  }

  // Rebuild the whole route geometry for the current snapped points.
  private recalculateRouteForAllPoints(): void {
    this.predictedSeconds = null;

    const hadOutAndBack =
      this.routeSegments.length > 0 && this.routeSegments[this.routeSegments.length - 1].isOutAndBack;

    if (this.snappedPoints.length < 2) {
      this.routeSegments = [];
      this.fullRouteCoords = [];
      this.updateRouteSourceEmpty();
      this.clearProfiles();
      return;
    }

    const pairs: { from: { lat: number; lon: number }; to: { lat: number; lon: number } }[] = [];
    for (let i = 1; i < this.snappedPoints.length; i++) {
      pairs.push({
        from: this.snappedPoints[i - 1],
        to: this.snappedPoints[i]
      });
    }

    const url = `${this.apiBase}/api/routing/route`;

    const newSegments: RouteSegment[] = [];
    const newFull: RouteCoordinateDto[] = [];

    const doNext = (i: number) => {
      if (i >= pairs.length) {
        if (hadOutAndBack && newFull.length > 1) {
          const reversed = [...newFull].reverse().slice(1);
          newSegments.push({ coords: reversed, isOutAndBack: true });
          newFull.push(...reversed);
        }

        this.routeSegments = newSegments;
        this.fullRouteCoords = newFull;

        this.updateRouteOnMap();
        this.recomputeDistanceProfile();
        this.fetchElevationProfile();
        return;
      }

      const { from, to } = pairs[i];
      const body: RouteRequestDto = {
        fromLat: from.lat,
        fromLon: from.lon,
        toLat: to.lat,
        toLon: to.lon
      };

      this.http.post<RouteCoordinateDto[]>(url, body).subscribe({
        next: (coords) => {
          if (coords && coords.length > 0) {
            let segmentCoords = coords;

            if (i > 0) {
              segmentCoords = coords.slice(1);
            }

            newSegments.push({ coords: segmentCoords, isOutAndBack: false });

            if (i === 0) {
              newFull.push(...coords);
            } else {
              newFull.push(...segmentCoords);
            }
          } else {
            console.warn('Empty route segment during recalc for pair', i);
          }

          doNext(i + 1);
        },
        error: (err) => {
          console.error('Recalculate route failed for pair', i, err);
        }
      });
    };

    doNext(0);
  }

  private rebuildRouteFromSegments(): void {
    this.predictedSeconds = null;

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
    if (n === 0) {
      this.totalDistanceMeters = 0;
      this.distanceProfile = [];
      return;
    }

    this.totalDistanceMeters = 0;
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
          tension: 0.2
        }
      ]
    };

    this.recomputeAscentDescent();

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

  private recomputeAscentDescent(): void {
    const n = this.elevationProfile.length;

    if (n === 0) {
      this.totalAscentMeters = 0;
      this.totalDescentMeters = 0;
      return;
    }

    let ascent = 0;
    let descent = 0;

    for (let i = 1; i < n; i++) {
      const prev = this.elevationProfile[i - 1];
      const curr = this.elevationProfile[i];

      if (!Number.isFinite(prev) || !Number.isFinite(curr)) {
        continue;
      }

      const diff = curr - prev;
      if (diff > 0) {
        ascent += diff;
      } else if (diff < 0) {
        descent -= diff; // diff is negative
      }
    }

    this.totalAscentMeters = Math.round(ascent);
    this.totalDescentMeters = Math.round(descent);
  }


  // ---------- Toolbar actions ----------

  undoLastSection(): void {
    if (!this.undoService.canUndo()) {
      return;
    }

    this.undoService.undoLast(this.buildUndoContext());
  }

  private buildUndoContext(): UndoContext {
    return {
      snappedPoints: this.snappedPoints,
      routeSegments: this.routeSegments,
      fullRouteCoords: this.fullRouteCoords,
      updatePointsSource: () => this.updatePointsSource(),
      updateRouteSourceEmpty: () => this.updateRouteSourceEmpty(),
      rebuildRouteFromSegments: () => this.rebuildRouteFromSegments(),
      clearProfiles: () => this.clearProfiles(),
      recalculateRouteForAllPoints: () => this.recalculateRouteForAllPoints()
    };
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
    this.undoService.reset();
    this.predictedSeconds = null;

    this.daylightInfo = null;
    this.clearDaylightMarkers();
  }


  // ---------- Import GPX ----------
  onGpxFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      return;
    }

    const file = input.files[0];

    // Optional: allow re-selecting the same file later
    input.value = '';

    this.gpxImportService.parseGpx(file)
      .then(coords => {
        if (!coords || coords.length < 2) {
          alert('The GPX file does not contain enough track points.');
          return;
        }

        // 1) Clear everything currently on the map / state
        this.clearAll();

        // 2) Use the GPX points as the route geometry directly
        this.fullRouteCoords = coords.map(c => ({
          lat: c.lat,
          lon: c.lon
        }));

        this.routeSegments = [
          {
            coords: this.fullRouteCoords,
            isOutAndBack: false
          }
        ];

        // 3) Put 2 circles at the ends of the route
        const start = this.fullRouteCoords[0];
        const end = this.fullRouteCoords[this.fullRouteCoords.length - 1];

        this.snappedPoints = [
          { lat: start.lat, lon: start.lon },
          { lat: end.lat, lon: end.lon }
        ];

        // 4) Refresh map sources + stats
        this.updateRouteOnMap();
        this.updatePointsSource();
        this.recomputeDistanceProfile();
        this.fetchElevationProfile();
      })
      .catch(err => {
        console.error('Failed to import GPX', err);
        alert('Could not read GPX file.');
      });
  }

  // --------- Predict ---------

  predictTime(): void {
    if (this.fullRouteCoords.length < 2) return;

    this.isPredicting = true;
    this.daylightInfo = null;
    this.clearDaylightMarkers();

    const startIso = this.solarService.buildStartIso(
      this.startDateInput,
      this.startTimeInput
    );

    this.solarService
      .predictDaylight({
        points: this.fullRouteCoords.map(c => ({ lat: c.lat, lon: c.lon })),
        startLocalTime: startIso
      })
      .subscribe({
        next: (res) => {
          this.isPredicting = false;
          this.daylightInfo = res;
          this.predictedSeconds = res.predictedSeconds;
          this.updateDaylightMarkersOnMap();
        },
        error: (err) => {
          this.isPredicting = false;
          console.error(err);
        }
      });
  }



  formatPredictedTime(): string {
    if (this.predictedSeconds == null) {
      return '';
    }

    const total = Math.round(this.predictedSeconds);
    const hours = Math.floor(total / 3600);
    const minutes = Math.floor((total % 3600) / 60);

    if (hours <= 0) {
      return `${minutes} min`;
    }
    if (minutes === 0) {
      return `${hours} h`;
    }
    return `${hours} h ${minutes} min`;
  }

  private clearDaylightMarkers(): void {
    if (this.sunriseMarker) {
      this.sunriseMarker.remove();
      this.sunriseMarker = null;
    }
    if (this.sunsetMarker) {
      this.sunsetMarker.remove();
      this.sunsetMarker = null;
    }
  }

  private updateDaylightMarkersOnMap(): void {
    if (!this.map) {
      return;
    }

    this.clearDaylightMarkers();
    if (!this.daylightInfo) {
      return;
    }

    if (this.daylightInfo.sunriseMarker?.progress != null) {
      console.log(this.daylightInfo);
      const m = this.daylightInfo.sunriseMarker;
      const el = document.createElement('div');
      el.className = 'sun-marker';
      this.sunriseMarker = new mapboxgl.Marker(el)
        .setLngLat([m.lon, m.lat])
        .addTo(this.map);
    }

    if (this.daylightInfo.sunsetMarker?.progress != null) {
      const m = this.daylightInfo.sunsetMarker;
      const el = document.createElement('div');
      el.className = 'moon-marker';

      this.sunsetMarker = new mapboxgl.Marker(el)
        .setLngLat([m.lon, m.lat])
        .addTo(this.map);
    }
  }

  // ------------- Save route -------------------
  saveRoute(): void {
    if (this.fullRouteCoords.length < 2) {
      return;
    }

    const today = new Date();
    const iso = today.toISOString().substring(0, 10);
    this.saveFileName = `route-${iso}`;

    this.showSaveModal = true;
    this.setModalOpenState(true);
  }

  confirmSaveRoute(): void {
    const name = this.saveFileName?.trim();
    if (!name || this.fullRouteCoords.length < 2) {
      return;
    }

    const body: SavePlannedRouteDto = {
      fileName: name,
      points: this.fullRouteCoords.map(c => ({ lat: c.lat, lon: c.lon }))
    };

    const url = `${this.apiBase}/api/routing/save-planned-gpx`;

    this.isSavingRoute = true;

    this.http.post<void>(url, body).subscribe({
      next: () => {
        this.isSavingRoute = false;
        this.showSaveModal = false;
        this.setModalOpenState(false);
        alert(`Route saved as "${name}.gpx" in your Tracks folder.`);
      },
      error: (err) => {
        this.isSavingRoute = false;
        console.error('Save route failed', err);
        alert('Could not save the route.');
      }
    });
  }

  cancelSaveRoute(): void {
    this.showSaveModal = false;
    this.setModalOpenState(false);
  }


  private setModalOpenState(isOpen: boolean): void {
    if (!this.map) {
      return;
    }

    const canvas = this.map.getCanvas();
    if (canvas) {
      canvas.style.pointerEvents = isOpen ? 'none' : 'auto';
    }
  }


  // --------- search ---------------
  private setupSearch(): void {
    this.searchSub = this.searchControl.valueChanges.pipe(
      debounceTime(250),
      distinctUntilChanged(),
      tap(() => {
        this.isSearching = true;
        this.showSearchResults = true;
      }),
      filter(q => !!q && q.trim().length > 0),
      switchMap(q => {
        const trimmed = q!.trim();
        const center = this.map?.getCenter();
        const mapCenter = center ? { lat: center.lat, lon: center.lng } : undefined;
        return this.geocodeService.search(trimmed, mapCenter).pipe(
          finalize(() => this.isSearching = false)
        );
      })
    ).subscribe({
      next: (results) => {
        this.searchResults = results;
      },
      error: (err) => {
        console.error('Geocode error', err);
        this.searchResults = [];
        this.isSearching = false;
      }
    });
  }

  selectSearchResult(place: PlaceSearchResult): void {
    this.showSearchResults = false;
    this.searchResults = [];
    this.searchControl.setValue(place.name, { emitEvent: false });

    const center: [number, number] = [place.lon, place.lat];

    if (this.map) {
      this.map.flyTo({
        center,
        zoom: 15
      });
    }
  }

  clearSearchResults(): void {
    this.showSearchResults = false;
    this.searchResults = [];
  }




  ngOnDestroy(): void {
    if (this.map) {
      this.map.remove();
    }
    this.searchSub?.unsubscribe();
  }

  // Temporary: request offline map export for a hardcoded track file ("Panorámakör")
  downloadOfflineMap_Panoramakor(): void {
    const url = `${this.apiBase}/api/mapsforge/from-track-file`;

    // Adjust property name if your controller expects something else
    const body = { trackFileName: 'Panorámakör' };

    this.http.post(url, body, { responseType: 'blob', observe: 'response' }).subscribe({
      next: (res) => {
        const blob = res.body;
        if (!blob) {
          alert('No file received.');
          return;
        }

        // Try to pick filename from Content-Disposition; fallback if missing
        const cd = res.headers.get('content-disposition') ?? '';
        const fileName =
          this.tryGetFilenameFromContentDisposition(cd) ?? 'Panoramakor.map';

        this.downloadBlob(blob, fileName);
      },
      error: (err) => {
        console.error('Mapsforge export failed', err);
        alert('Could not export map.');
      }
    });
  }

  private downloadBlob(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    a.click();
    URL.revokeObjectURL(url);
  }

  private tryGetFilenameFromContentDisposition(cd: string): string | null {
    // Example: Content-Disposition: attachment; filename="track_user_....map"
    const match = /filename\*?=(?:UTF-8''|")?([^\";]+)"?/i.exec(cd);
    if (!match?.[1]) return null;

    // decode RFC5987 if present (best-effort)
    try { return decodeURIComponent(match[1]); } catch { return match[1]; }
  }



}