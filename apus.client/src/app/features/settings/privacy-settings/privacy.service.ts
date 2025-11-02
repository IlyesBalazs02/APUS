import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

export interface PrivacyDto {
    allowFollow: boolean;
    activityVisibility: string;
    profileVisibility: string;
}

@Injectable({ providedIn: 'root' })
export class PrivacyService {
    private base = '/api/privacy';
    constructor(private http: HttpClient) { }
    getMine() { return this.http.get<PrivacyDto>(this.base); }
    updateMine(dto: PrivacyDto) { return this.http.put<PrivacyDto>(this.base, dto); }
}
