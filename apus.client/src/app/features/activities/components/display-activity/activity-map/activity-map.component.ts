import { Component, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import * as mapboxgl from 'mapbox-gl';
import { environment } from '../../../../../../environments/environment';
import { Trackpoint } from '../../../ActivityDto/TrackpointDto';

@Component({
  selector: 'app-activity-map',
  standalone: false,
  template: `
    <div class="map-wrapper">
      <label class="map-toggle">
        <input
          type="checkbox"
          [checked]="showImages"
          (change)="onShowImagesToggle($event)"
        />
        Show pictures
      </label>
      <div id="map" class="map-container"></div>
    </div>
  `,
  styles: [
    `
    .map-wrapper {
      position: relative;
    }

    .map-toggle {
      position: absolute;
      z-index: 1;
      top: 8px;
      left: 8px;
      background: rgba(255, 255, 255, 0.9);
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 12px;
      box-shadow: 0 1px 4px rgba(0, 0, 0, 0.25);
      user-select: none;
    }

    .map-toggle input {
      margin-right: 4px;
    }

    .map-container {
      width: 100%;
      height: 400px;
    }
    `
  ]
})
export class ActivityMapComponent implements OnInit, OnChanges {
  @Input() trackpoints: Trackpoint[] = [];

  @Input() imagePoints: { lat: number; lon: number; url: string }[] = [];

  private map?: mapboxgl.Map;

  private readonly style = 'mapbox://styles/mapbox/streets-v11';
  private readonly routeSourceId = 'route';
  private readonly routeLayerId = 'route-layer';

  private readonly imageSourceId = 'activity-images';
  private readonly imageLayerId = 'activity-images-layer';

  // Track which image IDs it has already added to the map style
  private loadedImageIds = new Set<string>();

  // Checkbox state
  showImages = false;

  ngOnInit(): void {
    const firstValid = this.trackpoints.find(tp => tp.lat != null && tp.lon != null);

    if (firstValid) {
      this.initMap(firstValid.lon!, firstValid.lat!);
    } else {
      // Fallback center if no trackpoints
      this.initMap(19.0402, 47.4979);
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.map) {
      return;
    }

    const trackpointsChanged = !!changes['trackpoints'];
    const imagesChanged = !!changes['imagePoints'];

    if (trackpointsChanged) {
      this.updateRoute();

      const firstValid = this.trackpoints.find(tp => tp.lat != null && tp.lon != null);
      if (firstValid) {
        this.map.flyTo({ center: [firstValid.lon!, firstValid.lat!], zoom: 13 });
      }
    }

    if (imagesChanged) {
      this.updateImageLayer();
    }
  }

  private initMap(lon: number, lat: number): void {
    this.map = new mapboxgl.Map({
      accessToken: environment.mapbox.accessToken,
      container: 'map',
      style: this.style,
      center: [lon, lat],
      zoom: 13,
      pitch: 45, // 3D effect
      bearing: -17.6
    });

    this.map.on('load', () => {
      this.updateRoute();
      this.updateImageLayer();
    });
  }

  // ROUTE
  private updateRoute(): void {
    if (!this.map || !this.map.isStyleLoaded()) {
      return;
    }

    const coords = this.trackpoints
      .filter(tp => tp.lat != null && tp.lon != null)
      .map(tp => [tp.lon!, tp.lat!] as [number, number]);

    if (!coords.length) {
      const existingLayer = this.map.getLayer(this.routeLayerId);
      if (existingLayer) this.map.removeLayer(this.routeLayerId);

      const existingSource = this.map.getSource(this.routeSourceId);
      if (existingSource) this.map.removeSource(this.routeSourceId);

      return;
    }

    const geojson: GeoJSON.Feature<GeoJSON.LineString> = {
      type: 'Feature',
      geometry: {
        type: 'LineString',
        coordinates: coords
      },
      properties: {}
    };

    const existingSource = this.map.getSource(this.routeSourceId) as mapboxgl.GeoJSONSource | undefined;

    if (existingSource) {
      existingSource.setData(geojson);
    } else {
      this.map.addSource(this.routeSourceId, {
        type: 'geojson',
        data: geojson
      });

      this.map.addLayer({
        id: this.routeLayerId,
        type: 'line',
        source: this.routeSourceId,
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

  // SHOW / HIDE IMAGES (checkbox)
  onShowImagesToggle(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.showImages = input.checked;

    if (!this.map) return;

    if (!this.showImages) {
      const existingLayer = this.map.getLayer(this.imageLayerId);
      if (existingLayer) this.map.removeLayer(this.imageLayerId);

      const existingSource = this.map.getSource(this.imageSourceId);
      if (existingSource) this.map.removeSource(this.imageSourceId);
      return;
    }

    // Recreate
    this.updateImageLayer();
  }

  private updateImageLayer(): void {
    if (!this.map || !this.map.isStyleLoaded()) {
      return;
    }

    if (!this.showImages) {
      const existingLayer = this.map.getLayer(this.imageLayerId);
      if (existingLayer) this.map.removeLayer(this.imageLayerId);

      const existingSource = this.map.getSource(this.imageSourceId);
      if (existingSource) this.map.removeSource(this.imageSourceId);

      return;
    }

    if (!this.imagePoints || !this.imagePoints.length) {
      const existingLayer = this.map.getLayer(this.imageLayerId);
      if (existingLayer) this.map.removeLayer(this.imageLayerId);

      const existingSource = this.map.getSource(this.imageSourceId);
      if (existingSource) this.map.removeSource(this.imageSourceId);

      return;
    }

    // max 2 pictures if they are very close
    const effectivePoints = this.getThinnedImagePoints();
    if (!effectivePoints.length) {
      const existingLayer = this.map.getLayer(this.imageLayerId);
      if (existingLayer) this.map.removeLayer(this.imageLayerId);

      const existingSource = this.map.getSource(this.imageSourceId);
      if (existingSource) this.map.removeSource(this.imageSourceId);

      return;
    }

    const features: GeoJSON.Feature<GeoJSON.Point>[] = effectivePoints.map((p, index) => {
      const iconId = this.getIconId(index);
      return {
        type: 'Feature',
        geometry: {
          type: 'Point',
          coordinates: [p.lon, p.lat]
        },
        properties: {
          iconId,
          url: p.url
        }
      };
    });

    const collection: GeoJSON.FeatureCollection<GeoJSON.Point> = {
      type: 'FeatureCollection',
      features
    };

    const existingSource = this.map.getSource(this.imageSourceId) as mapboxgl.GeoJSONSource | undefined;
    if (existingSource) {
      existingSource.setData(collection);
    } else {
      this.map.addSource(this.imageSourceId, {
        type: 'geojson',
        data: collection
      });
    }

    this.ensureMapImagesLoaded(collection);

    if (!this.map.getLayer(this.imageLayerId)) {
      this.map.addLayer({
        id: this.imageLayerId,
        type: 'symbol',
        source: this.imageSourceId,
        layout: {
          'icon-image': ['get', 'iconId'],
          'icon-allow-overlap': true,

          // Keep icons facing the user
          'icon-pitch-alignment': 'viewport',
          'icon-rotation-alignment': 'viewport',

          'icon-size': [
            'interpolate',
            ['exponential', 1.2],
            ['zoom'],
            10, 0.05,
            14, 0.20,
            18, 0.90
          ]
        }
      } as any);
    }
  }

  private getThinnedImagePoints(): { lat: number; lon: number; url: string }[] {
    const maxPerCluster = 2;
    const clusterDistanceMeters = 40; // Max how close can pictures be

    const clusters: {
      centerLat: number;
      centerLon: number;
      points: { lat: number; lon: number; url: string }[];
    }[] = [];

    for (const p of this.imagePoints) {
      let targetCluster = clusters.find(
        c =>
          this.distanceMeters(c.centerLat, c.centerLon, p.lat, p.lon) <
          clusterDistanceMeters
      );

      if (!targetCluster) {
        clusters.push({
          centerLat: p.lat,
          centerLon: p.lon,
          points: [p]
        });
      } else if (targetCluster.points.length < maxPerCluster) {
        targetCluster.points.push(p);

        const n = targetCluster.points.length;
        targetCluster.centerLat =
          targetCluster.points.reduce((sum, pt) => sum + pt.lat, 0) / n;
        targetCluster.centerLon =
          targetCluster.points.reduce((sum, pt) => sum + pt.lon, 0) / n;
      } else {
      }
    }

    return clusters.flatMap(c => c.points);
  }

  private distanceMeters(
    lat1: number,
    lon1: number,
    lat2: number,
    lon2: number
  ): number {
    const R = 6371000;
    const toRad = (v: number) => (v * Math.PI) / 180;

    const dLat = toRad(lat2 - lat1);
    const dLon = toRad(lon2 - lon1);

    const a =
      Math.sin(dLat / 2) * Math.sin(dLat / 2) +
      Math.cos(toRad(lat1)) *
      Math.cos(toRad(lat2)) *
      Math.sin(dLon / 2) *
      Math.sin(dLon / 2);

    const c = 2 * Math.asin(Math.sqrt(a));
    return R * c;
  }

  // Load images, draw square thumbnail + border
  private ensureMapImagesLoaded(
    collection: GeoJSON.FeatureCollection<GeoJSON.Point>
  ): void {
    if (!this.map) return;

    for (const feature of collection.features) {
      const props = feature.properties as any;
      const iconId: string = props.iconId;
      const url: string = props.url;

      if (!iconId || !url) continue;

      if (this.loadedImageIds.has(iconId) || this.map.hasImage(iconId)) {
        this.loadedImageIds.add(iconId);
        continue;
      }

      const img = new Image();
      img.crossOrigin = 'anonymous';
      img.onload = () => {
        if (!this.map || this.map.hasImage(iconId)) return;

        const size = 256; // thumbnail resolution

        const canvas = document.createElement('canvas');
        canvas.width = size;
        canvas.height = size;
        const ctx = canvas.getContext('2d');
        if (!ctx) return;

        const iw = img.naturalWidth;
        const ih = img.naturalHeight;

        const scale = Math.max(size / iw, size / ih);
        const scaledWidth = iw * scale;
        const scaledHeight = ih * scale;
        const dx = (size - scaledWidth) / 2;
        const dy = (size - scaledHeight) / 2;

        ctx.drawImage(img, dx, dy, scaledWidth, scaledHeight);

        ctx.strokeStyle = '#ffffff';
        ctx.lineWidth = 8;
        ctx.strokeRect(4, 4, size - 8, size - 8);

        const imageData = ctx.getImageData(0, 0, size, size);

        this.map.addImage(iconId, {
          width: imageData.width,
          height: imageData.height,
          data: imageData.data
        } as any);

        this.loadedImageIds.add(iconId);
      };

      img.onerror = () => {
        console.warn('Failed to load map icon image:', url);
      };

      img.src = url;
    }
  }

  private getIconId(index: number): string {
    return `activity-img-${index}`;
  }
}
