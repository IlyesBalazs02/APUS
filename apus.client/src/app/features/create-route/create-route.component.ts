import { AfterViewInit, Component, OnDestroy } from '@angular/core';
import * as mapboxgl from 'mapbox-gl';
import { environment } from '../../../environments/environment';
import { HttpClient, HttpParams } from '@angular/common/http';

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

  // Initial center (Budapest-ish)
  lat: number = 47.53277;
  lng: number = 19.052245;

  private markers: mapboxgl.Marker[] = [];
  private snappedPoints: { lat: number; lon: number }[] = [];
  private fullRouteCoords: RouteCoordinateDto[] = [];

  // if you already have environment.apiUrl, use it; otherwise '' keeps current behaviour
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
      console.log('[Map] load fired');
      this.addEmptyRouteSource();

      // sanity check: put a marker in the middle once on load
      this.addMarker(this.lat, this.lng);
    });

    this.map.on('click', (e) => this.onMapClick(e));
  }

  private onMapClick(e: mapboxgl.MapMouseEvent): void {
    console.log('[Map] click', e.lngLat);

    const { lng, lat } = e.lngLat;

    this.snappedPoints.push({ lat, lon: lng });

    const params = new HttpParams()
      .set('lat', lat.toString())
      .set('lon', lng.toString());

    const url = `${this.apiBase}/api/routing/snap`;
    console.log('[Snap] GET', url, 'params=', params.toString());

    this.http.get<SnapResponseDto>(url, { params })
      .subscribe({
        next: snap => {
          console.log('[Snap] result', snap);

          // Optionally: move the last marker to the snapped position.
          // For now, just add an extra snapped marker:
          this.addMarker(snap.lat, snap.lon);

          // replace last snapped point with the snapped one
          this.snappedPoints[this.snappedPoints.length - 1] = {
            lat: snap.lat,
            lon: snap.lon
          };

          // if we have at least two snapped points, route between the last two
          if (this.snappedPoints.length >= 2) {
            const count = this.snappedPoints.length;
            const from = this.snappedPoints[count - 2];
            const to = this.snappedPoints[count - 1];

            this.requestRoute(from, to);
          }
        },
        error: err => {
          console.error('[Snap] failed', err);
          // we already added a raw-click marker above, so just notify
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
    console.log('[Marker] add', { lat, lon });
    const marker = new mapboxgl.Marker()
      .setLngLat([lon, lat])
      .addTo(this.map);

    this.markers.push(marker);
  }

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
    console.log('[Route] POST', url, body);

    this.http.post<RouteCoordinateDto[]>(url, body)
      .subscribe({
        next: (coords) => {
          console.log('[Route] coords count', coords?.length ?? 0);

          if (!coords || coords.length === 0) {
            console.warn('Route returned no coordinates');
            return;
          }

          if (this.fullRouteCoords.length === 0) {
            this.fullRouteCoords = coords;
          } else {
            const toAppend = coords.slice(1);
            this.fullRouteCoords = this.fullRouteCoords.concat(toAppend);
          }

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
        },
        error: err => {
          console.error('Route request failed', err);
          alert('Could not calculate route (see console for details).');
        }
      });
  }
}
