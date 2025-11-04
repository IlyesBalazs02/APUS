import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

export interface UserSearchDto {
    id: string;
    fullName: string;
    userName: string | null;
    avatarUrl: string | null;
}

export interface PagedUsers {
    items: UserSearchDto[];
    hasMore: boolean;
}


export interface FriendStatusDto {
    userId: string;
    canRequest: boolean;
    reason?: string | null;
    existingStatus?: 'Pending' | 'Accepted' | 'Blocked' | null;
    direction?: 'Outgoing' | 'Incoming' | null;
}

// for paged responses
export interface PagedResponse<T> {
    items: T[];
    hasMore: boolean;
}

@Injectable({ providedIn: 'root' })
export class UserSearchApi {
    private readonly base = '/api/search/search-users';
    constructor(private http: HttpClient) { }

    search(query: string, skip: number, take: number): Observable<PagedResponse<UserSearchDto>> {
        const normalizedQuery = query?.trim() ?? '';

        // build query parameters
        const params = new HttpParams()
            .set('query', normalizedQuery)
            .set('skip', skip)
            .set('take', take);

        return this.http.get<any>(this.base, { params }).pipe(
            map(res => {
                // normalize response whether it comes from System.Text.Json or Newtonsoft
                const rawItems = res.items ?? res.Items ?? [];
                const items = Array.isArray(rawItems) ? rawItems : (rawItems?.$values ?? []);
                const hasMore = (res.hasMore ?? res.HasMore ?? false) as boolean;
                return { items, hasMore } as PagedResponse<UserSearchDto>;
            })
        );
    }


    getFriendStatuses(userIds: string[]) {
        return this.http.post<Record<string, FriendStatusDto>>(`/api/friends/status`, userIds);
    }

    sendFriendRequest(toUserId: string) {
        return this.http.post<void>(`/api/friends/request/${toUserId}`, {});
    }


}
