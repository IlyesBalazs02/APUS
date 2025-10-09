import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface UserMatch {
    id: string;
    userName: string;
    fullName: string;
    avatarUrl?: string | null;
}

@Injectable({ providedIn: 'root' })
export class FriendSearchService {
    constructor(private http: HttpClient) { }

    search(q: string, limit = 20): Observable<UserMatch[]> {
        const params = new HttpParams().set('q', q).set('limit', limit);
        var asd = this.http.get<UserMatch[]>('/api/friends/get-all-user', { params });
        console.log("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!" + asd);
        return asd;
    }
}