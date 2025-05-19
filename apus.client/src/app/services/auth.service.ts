import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { JwtResponse, LoginDto, RegisterDto } from '../auth/Dto/AuthDtos';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private baseUrl = 'https://localhost:7244/api/auth';

  constructor(private http: HttpClient) { }

  register(dto: RegisterDto): Observable<any> {
    return this.http.post(`${this.baseUrl}/register`, dto);
  }

  login(dto: LoginDto): Observable<JwtResponse> {
    return this.http.post<JwtResponse>(`${this.baseUrl}/login`, dto)
      .pipe(
        tap(res => localStorage.setItem('jwt', res.token))
      );
  }

  logout() {
    localStorage.removeItem('jwt');
  }

  getToken(): string | null {
    return localStorage.getItem('jwt');
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }
}
