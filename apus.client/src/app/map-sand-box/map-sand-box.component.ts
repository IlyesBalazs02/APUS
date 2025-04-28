import { AfterViewInit, Component } from '@angular/core';
import * as mapboxgl from 'mapbox-gl';

@Component({
  selector: 'app-map-sand-box',
  standalone: false,
  templateUrl: './map-sand-box.component.html',
  styleUrl: './map-sand-box.component.css'
})
export class MapSandBoxComponent implements AfterViewInit {
  private map!: mapboxgl.Map;

  private accessToken = 'YOUR_MAPBOX_ACCESS_TOKEN';

  ngAfterViewInit() {

  }
}
