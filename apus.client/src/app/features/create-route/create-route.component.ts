import { AfterViewInit, Component, OnInit } from '@angular/core';
import * as mapboxgl from 'mapbox-gl';
import { environment } from '../../../environments/environment';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-create-route',
  standalone: false,
  templateUrl: './create-route.component.html',
  styleUrls: ['./create-route.component.css']
})

export class CreateRouteComponent implements OnInit {
  map!: mapboxgl.Map;
  style = 'mapbox://styles/mapbox/streets-v11';
  lat: number = 47.532770;
  lng: number = 19.052245;

  selectedCoordinateType: 'start' | 'end' = 'start';
  coordinates = {
    start: { lat: 0, lon: 0 },
    end: { lat: 0, lon: 0 }
  };


  startMarker: mapboxgl.Marker | null = null;
  endMarker: mapboxgl.Marker | null = null;


  constructor(private http: HttpClient) { }

  ngOnInit(): void {
    this.map = new mapboxgl.Map({
      accessToken: environment.mapbox.accessToken,
      container: 'map',
      style: this.style,
      zoom: 13,
      center: [this.lng, this.lat]
    });

    this.map.dragPan.disable();



    let isRightClickDragging = false;
    let lastMousePos: { x: number; y: number } | null = null;

    this.map.getCanvas().addEventListener('mousedown', (e) => {
      if (e.button === 2) { // Right-click
        isRightClickDragging = true;
        lastMousePos = { x: e.clientX, y: e.clientY };
        this.map.getCanvas().style.cursor = 'grabbing';
      }
    });

    window.addEventListener('mousemove', (e) => {
      if (isRightClickDragging && lastMousePos) {
        const dx = e.clientX - lastMousePos.x;
        const dy = e.clientY - lastMousePos.y;

        const currentCenter = this.map.getCenter();
        const newCenter = this.map.unproject([
          this.map.project(currentCenter).x - dx,
          this.map.project(currentCenter).y - dy
        ]);

        this.map.setCenter(newCenter);
        lastMousePos = { x: e.clientX, y: e.clientY };
      }
    });

    window.addEventListener('mouseup', (e) => {
      if (e.button === 2) { // Right-click
        isRightClickDragging = false;
        this.map.getCanvas().style.cursor = '';
      }
    });

    this.map.on('click', (event) => {
      const { lng, lat } = event.lngLat;

      if (this.selectedCoordinateType === 'start') {
        this.coordinates.start = { lat, lon: lng };

        if (this.startMarker) {
          this.startMarker.remove();
        }

        this.startMarker = new mapboxgl.Marker({ color: 'green' })
          .setLngLat([lng, lat])
          .addTo(this.map);
      } else {
        this.coordinates.end = { lat, lon: lng };

        if (this.endMarker) {
          this.endMarker.remove();
        }

        this.endMarker = new mapboxgl.Marker({ color: 'red' })
          .setLngLat([lng, lat])
          .addTo(this.map);
      }

      // Auto-submit when both are set
      if (this.coordinates.start.lat !== null && this.coordinates.end.lat !== null) {
        this.submitCoordinates();
      }
    });


    this.map.getCanvas().addEventListener('contextmenu', (e) => e.preventDefault());

  }

  submitCoordinates(): void {
    if (!this.coordinates.start.lat || !this.coordinates.end.lat) {
      alert('Please select both coordinates first.');
      return;
    }

    console.log('Posting coordinates', this.coordinates);

    const body = {
      start: { latitude: this.coordinates.start.lat, longitude: this.coordinates.start.lon },
      end: { latitude: this.coordinates.end.lat, longitude: this.coordinates.end.lon }
    };

    const startTime = performance.now();

    this.http.post<[number, number][]>('/api/routing/route', body).subscribe(route => {
      const endTime = performance.now();

      const durationSec = (endTime - startTime) / 1000;
      console.log(route);
      console.log(`Request took ${durationSec.toFixed(3)} s`);
      this.DrawRoute(route);
    });
  }

  private DrawRoute(rawCoords: any[]): void {
    if (!Array.isArray(rawCoords) || rawCoords.length === 0) {
      console.error('DrawRoute expected non-empty array, got:', rawCoords);
      return;
    }

    const first = rawCoords[0] as any;
    let routeCoords: [number, number][];

    if (Array.isArray(first)) {
      routeCoords = (rawCoords as [number, number][]).map(
        ([lat, lon]) => [lon, lat]
      );
    } else if ('item1' in first && 'item2' in first) {
      routeCoords = (rawCoords as { item1: number, item2: number }[]).map(
        c => [c.item2, c.item1]
      );
    } else if ('latitude' in first && 'longitude' in first) {
      routeCoords = (rawCoords as any[]).map(
        c => [c.longitude, c.latitude] as [number, number]
      );
    } else if ('lat' in first && 'lon' in first) {
      routeCoords = (rawCoords as any[]).map(
        c => [c.lon, c.lat] as [number, number]
      );
    } else {
      console.error('Unrecognized coordinate format:', first);
      return;
    }

    const data = {
      type: 'Feature',
      properties: {},
      geometry: { type: 'LineString', coordinates: routeCoords }
    };

    const existingSource = this.map.getSource('route') as mapboxgl.GeoJSONSource;
    if (existingSource) {
      existingSource.setData({
        type: 'Feature',
        properties: {},
        geometry: {
          type: 'LineString',
          coordinates: routeCoords
        }
      });
      return;
    }

    this.map.addSource('route', {
      type: 'geojson',
      data: {
        type: 'Feature',
        properties: {},
        geometry: {
          type: 'LineString',
          coordinates: routeCoords
        }
      }
    });

    this.map.addLayer({
      id: 'route',
      type: 'line',
      source: 'route',
      layout: {
        'line-join': 'round',
        'line-cap': 'round'
      },
      paint: {
        'line-color': '#ff0000',
        'line-width': 4
      }
    });
  }

}