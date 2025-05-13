import { Component, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import * as mapboxgl from 'mapbox-gl';
import { environment } from '../../../../../environments/environment';
import { Trackpoint } from '../../../ActivityDto/TrackpointDto';

@Component({
  selector: 'app-activity-map',
  standalone: false,
  template: `<div id="map" class="map-container"></div>`,
  styles: [
    `.map-container {
        width: 100%; height: 400px;
      }`
  ]
})
export class ActivityMapComponent implements OnChanges {
  @Input() trackpoints: Trackpoint[] = [];

  private map?: mapboxgl.Map;
  private style = 'mapbox://styles/mapbox/streets-v11';

  // Correct default lat/lon
  private defaultLat = 30.2672;
  private defaultLon = -97.7431;

  ngOnChanges(changes: SimpleChanges) {
    if (!changes['trackpoints']) return;

    // find first valid point
    const firstValid = this.trackpoints.find(tp => tp.lat != null && tp.lon != null);

    // if we now have coords AND the map isn't built yet, initialize it
    if (firstValid && !this.map) {
      this.initMap(firstValid.lon!, firstValid.lat!);
    }
    // if we already have a map AND coords, just re-center & draw
    else if (firstValid && this.map) {
      this.map.flyTo({ center: [firstValid.lon!, firstValid.lat!], zoom: 13 });
      this.addPolyline();
    }
  }

  private initMap(lon: number, lat: number) {
    this.map = new mapboxgl.Map({
      accessToken: environment.mapbox.accessToken,
      container: 'map',
      style: this.style,
      zoom: 13,
      center: [lon, lat]
    });

    this.map.on('load', () => {
      this.addPolyline();
    });
  }

  addPolyline() {
    const coords = this.trackpoints
      .filter(tp => tp.lat != null && tp.lon != null)
      .map(tp => [tp.lon!, tp.lat!] as [number, number]);

    if (coords.length === 0 || !this.map) return;

    // Fly to the first valid coordinate
    this.map.flyTo({ center: coords[0], zoom: 13 });

    // If the source already exists, update it
    const existingSource = this.map.getSource('route') as mapboxgl.GeoJSONSource;
    if (existingSource) {
      existingSource.setData({
        type: 'Feature',
        properties: {},
        geometry: {
          type: 'LineString',
          coordinates: coords
        }
      });
      return;
    }

    // Otherwise, add source and layer
    this.map.addSource('route', {
      type: 'geojson',
      data: {
        type: 'Feature',
        properties: {},
        geometry: {
          type: 'LineString',
          coordinates: coords
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
