import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

export interface FriendRequestItemDto {
    fromUserId: string;
    fromFullName: string;
    fromAvatarUrl?: string | null;
}

@Injectable({ providedIn: 'root' })
export class FriendRequestsApi {
    constructor(private http: HttpClient) { }

    getIncoming() {
        return this.http.get<FriendRequestItemDto[]>(`/api/friends/requests`);
    }
    accept(fromUserId: string) {
        return this.http.post<void>(`/api/friends/requests/${fromUserId}/accept`, {});
    }
    reject(fromUserId: string) {
        return this.http.post<void>(`/api/friends/requests/${fromUserId}/reject`, {});
    }
}
