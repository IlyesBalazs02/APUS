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

  constructor(private http: HttpClient) { }

  ngOnInit(): void {
    this.map = new mapboxgl.Map({
      accessToken: environment.mapbox.accessToken,
      container: 'map',
      style: this.style,
      zoom: 13,
      center: [this.lng, this.lat]
    });
    const body = {
      start: { latitude: 2, longitude: 2 },
      end: { latitude: 2, longitude: 2 }
    };


    this.http.post<[number, number][]>('/api/routing/route', body).subscribe();
  }
}