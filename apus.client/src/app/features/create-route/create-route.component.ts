import { AfterViewInit, Component, OnDestroy, OnInit } from '@angular/core';
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

  constructor(private http: HttpClient) {
  }

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
      this.addEmptyRouteSource();
      this.registerMapClickHandler();
    });
  }

  private addEmptyRouteSource(): void {
    // Empty feature collection for the route
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

  private registerMapClickHandler(): void {
    this.map.on('click', (e) => {
      const { lng, lat } = e.lngLat;

      // 1) Snap clicked position to nearest road via backend
      const params = new HttpParams()
        .set('lat', lat.toString())
        .set('lon', lng.toString());

      this.http.get<SnapResponseDto>('/api/routing/snap', { params })
        .subscribe({
          next: snap => {
            // add marker at snapped coordinate
            this.addMarker(snap.lat, snap.lon);

            // remember snapped point
            this.snappedPoints.push({ lat: snap.lat, lon: snap.lon });

            // if we have at least two snapped points, route between the last two
            if (this.snappedPoints.length >= 2) {
              const count = this.snappedPoints.length;
              const from = this.snappedPoints[count - 2];
              const to = this.snappedPoints[count - 1];

              this.requestRoute(from, to);
            }
          },
          error: err => {
            console.error('Snap failed', err);
            alert('Could not snap to road (see console for details).');
          }
        });
    });
  }

  private addMarker(lat: number, lon: number): void {
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

    this.http.post<RouteCoordinateDto[]>('/api/routing/route', body)
      .subscribe({
        next: (coords) => {
          if (!coords || coords.length === 0) {
            console.warn('Route returned no coordinates');
            return;
          }

          // First segment: just set it
          if (this.fullRouteCoords.length === 0) {
            this.fullRouteCoords = coords;
          } else {
            // Next segments: append, but skip the first point
            // (it’s the same as the last point of the previous segment)
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
