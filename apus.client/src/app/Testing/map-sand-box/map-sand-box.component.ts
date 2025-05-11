import { AfterViewInit, Component, OnInit } from '@angular/core';
import * as mapboxgl from 'mapbox-gl';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-map-sand-box',
  template: `<div id="map" class="map-container"></div>`,
  styles: [
    `.map-container {
        width: 100%; height: 400px;
      }`
  ]
})
export class MapSandBoxComponent implements OnInit {
  /* map!: mapboxgl.Map;
 
   ngAfterViewInit(): void {
     // Set your access token
     mapboxgl.default.accessToken = environment.mapbox.accessToken;
 
     // Initialize the map
     this.map = new mapboxgl.Map({
       container: 'map', // ID of the HTML element to display the map
       style: 'mapbox://styles/mapbox/streets-v11', // Map style
       center: [19.0402, 47.4979], // Coordinates of Budapest (Longitude, Latitude)
       zoom: 12 // Zoom level
     });
 
     // Add navigation controls
     this.map.addControl(new mapboxgl.NavigationControl());
   }*/

  map: mapboxgl.Map | undefined;
  style = 'mapbox://styles/mapbox/streets-v11';
  lat: number = 30.2672;
  lng: number = -97.7431;

  ngOnInit() {
    this.map = new mapboxgl.Map({
      accessToken: environment.mapbox.accessToken,
      container: 'map',
      style: this.style,
      zoom: 13,
      center: [this.lng, this.lat]
    });
  }

}
