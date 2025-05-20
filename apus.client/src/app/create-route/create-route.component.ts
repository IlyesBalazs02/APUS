import { AfterViewInit, Component, OnInit } from '@angular/core';
import * as mapboxgl from 'mapbox-gl';
import { environment } from '../../environments/environment';
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
    start: { lat: null as number | null, lon: null as number | null },
    end: { lat: null as number | null, lon: null as number | null }
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

    // Post to your backend route
    console.log('Posting coordinates', this.coordinates);

    const body = {
      start: { latitude: this.coordinates.start.lat, longitude: this.coordinates.start.lon },
      end: { latitude: this.coordinates.end.lat, longitude: this.coordinates.end.lon }
    };

    this.http.post<[number, number][]>('/api/routing/route', body).subscribe(route => {
      console.log('Received route:', route);
    });
  }
}