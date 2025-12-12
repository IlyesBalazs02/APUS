import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { map, Observable } from "rxjs";
import { MainActivity } from "../../features/activities/_models/ActivityClasses";
import { ActivityDto } from "../../features/activities/ActivityDto/ActivityDto";
import { PagedResponse } from "../../shared/DTOs/PagedResponse";

@Injectable({ providedIn: 'root' })
export class ActivityService {
    private readonly apiUrl = '/api/activities';

    constructor(private http: HttpClient) { }

    getActivitiesDto(): Observable<ActivityDto[]> {
        return this.http
            .get<ActivityDto[]>(`${this.apiUrl}/get-activities`);
    }

    //Create a new service
    getUserActivities(): Observable<ActivityDto[]> {
        return this.http
            .get<ActivityDto[]>(`${this.apiUrl}/get-user-activities`);
    }

    getUserActivitiesById(id: string): Observable<ActivityDto[]> {
        return this.http.get<ActivityDto[]>(`${this.apiUrl}/user/${id}`);
    }

    // Global feed (newest first; server returns take+1 to compute hasMore)
    getActivitiesPaged(skip: number, take = 10): Observable<PagedResponse<ActivityDto>> {
        const params = new HttpParams().set('skip', skip).set('take', take);
        return this.http.get<PagedResponse<ActivityDto>>('/api/activities/paged', { params });
    }

    // Current user's activities
    getMyActivitiesPaged(skip: number, take = 10): Observable<PagedResponse<ActivityDto>> {
        const params = new HttpParams().set('skip', skip).set('take', take);
        return this.http.get<PagedResponse<ActivityDto>>('/api/activities/me/paged', { params });
    }

    // Specific user's activities
    getUserActivitiesPaged(userId: string, skip: number, take = 10): Observable<PagedResponse<ActivityDto>> {
        const params = new HttpParams().set('skip', skip).set('take', take);
        return this.http.get<PagedResponse<ActivityDto>>(`/api/activities/user/${userId}/paged`, { params });
    }


}