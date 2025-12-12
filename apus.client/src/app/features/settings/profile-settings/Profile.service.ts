// src/app/features/settings/profile/profile.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ProfileDto {
    firstName: string;
    lastName: string;
    bio: string;
    avatarUrl?: string | null;
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

    uploadAvatar(file: File): Observable<{ url: string }> {
        const formData = new FormData();
        formData.append('file', file);
        return this.http.post<{ url: string }>(`${this.baseUrl}/upload-avatar`, formData);
    }

    deleteAvatar(): Observable<{ message: string }> {
        return this.http.delete<{ message: string }>(`${this.baseUrl}/delete-avatar`);
    }
}
