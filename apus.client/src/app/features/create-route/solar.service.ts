import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';


export interface DaylightMarkerDto {
    lat: number;
    lon: number;
    progress: number;
    secondsFromStart: number;
}

export interface DaylightResponseDto {
    predictedSeconds: number;
    startTime: string;
    finishTime: string;
    sunrise: string;
    sunset: string;
    percentBeforeNightfall: number;
    sunriseMarker?: DaylightMarkerDto | null;
    sunsetMarker?: DaylightMarkerDto | null;
}

export interface RouteCoordinateDto {
    lat: number;
    lon: number;
}

export interface DaylightRequestDto {
    points: RouteCoordinateDto[];
    startLocalTime?: string | null;
}

// ------------------------------------------------------

@Injectable({
    providedIn: 'root'
})
export class solarService {
    private readonly baseUrl = `${environment.apiBase}/api/routing`;

    constructor(private http: HttpClient) { }

    predictDaylight(request: DaylightRequestDto) {
        return this.http.post<DaylightResponseDto>(
            `${this.baseUrl}/predict-daylight`,
            request
        );
    }

    buildStartIso(date: string | null, time: string | null): string | null {
        if (!date || !time) {
            return null;
        }

        const d = new Date(`${date}T${time}:00`);
        return d.toISOString();
    }
}
