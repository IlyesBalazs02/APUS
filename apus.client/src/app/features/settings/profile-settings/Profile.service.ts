// src/app/features/settings/profile/profile.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ProfileDto {
    firstName: string;
    lastName: string;
    bio: string;
}

@Injectable({ providedIn: 'root' })
export class ProfileService {
    private baseUrl = '/api/Profile';

    constructor(private http: HttpClient) { }

    getProfile(): Observable<ProfileDto> {
        return this.http.get<ProfileDto>(`${this.baseUrl}/get-profile`);
    }

    updateProfile(payload: ProfileDto): Observable<{ message: string }> {
        return this.http.post<{ message: string }>(`${this.baseUrl}/update-profile`, payload);
    }
}
