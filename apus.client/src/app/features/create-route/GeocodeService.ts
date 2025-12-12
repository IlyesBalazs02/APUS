import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface PlaceSearchResult {
    id: number;
    name: string;
    class: string;
    type: string;
    importance: number;
    lat: number;
    lon: number;
}

@Injectable({ providedIn: 'root' })
export class GeocodeService {
    private readonly apiBase = (environment as any).apiUrl ?? '';
    private readonly geocodeUrl = `${this.apiBase}/api/geocode`;

    constructor(private http: HttpClient) { }

    search(query: string, center?: { lat: number; lon: number }): Observable<PlaceSearchResult[]> {
        let params = new HttpParams().set('q', query);

        if (center) {
            params = params
                .set('lat', center.lat.toString())
                .set('lon', center.lon.toString());
        }

        return this.http.get<PlaceSearchResult[]>(this.geocodeUrl, { params });
    }
}
