import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AccountService {
    private baseUrl = '/api/Account';

    constructor(private http: HttpClient) { }

    changeEmail(password: string, newEmail: string): Observable<any> {
        return this.http.post(`${this.baseUrl}/change-email`, { password, newEmail });
    }

    changePassword(currentPassword: string, newPassword: string): Observable<any> {
        return this.http.post(`${this.baseUrl}/change-password`, { currentPassword, newPassword });
    }

    changeGender(selectedGender: string): Observable<any> {
        return this.http.post(`${this.baseUrl}/change-gender`, { selectedGender });
    }
}
