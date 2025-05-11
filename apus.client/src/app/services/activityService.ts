import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { map, Observable } from "rxjs";
import { MainActivity } from "../activities/_models/ActivityClasses";
import { ActivityDto } from "../activities/ActivityDto/ActivityDto";

@Injectable({ providedIn: 'root' })
export class ActivityService {
    private readonly apiUrl = '/api/activities';

    constructor(private http: HttpClient) { }

    getActivitiesDto(): Observable<ActivityDto[]> {
        return this.http
            .get<ActivityDto[]>(`${this.apiUrl}/get-activities`);
    }
}