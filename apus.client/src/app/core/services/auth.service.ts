import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs/operators';
import { BehaviorSubject, Observable } from 'rxjs';
import { JwtResponse, LoginDto, RegisterDto } from '../../features/auth/Dto/AuthDtos';
import { Router } from '@angular/router';

const TOKEN_KEY = 'jwt';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private baseUrl = 'https://localhost:54954/api/auth';
  private logoutTimer: any;

  private _loggedIn$ = new BehaviorSubject<boolean>(!this.isTokenExpired());
  public loggedIn$: Observable<boolean> = this._loggedIn$.asObservable();

  constructor(private http: HttpClient, private router: Router) {
    // after hard refresh, schedule auto logout if needed
    this.scheduleAutoLogout();
  }

  register(dto: RegisterDto): Observable<any> {
    return this.http.post(`${this.baseUrl}/register`, dto);
  }

  login(dto: LoginDto): Observable<JwtResponse> {
    return this.http.post<JwtResponse>(`${this.baseUrl}/login`, dto).pipe(
      tap(res => {
        this.setToken(res.token);
      })
    );
  }

  setToken(token: string) {
    localStorage.setItem(TOKEN_KEY, token);
    this._loggedIn$.next(true);
    this.scheduleAutoLogout();
  }

  logout() {
    localStorage.removeItem(TOKEN_KEY);
    if (this.logoutTimer) clearTimeout(this.logoutTimer);
    this._loggedIn$.next(false);
  }

  logoutAndRedirect() {
    this.logout();
    this.router.navigate(['/login'], { queryParams: { reason: 'expired' } });
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  isLoggedIn(): boolean {
    return !this.isTokenExpired();
  }

  isTokenExpired(): boolean {
    const token = this.getToken();
    if (!token) return true;
    const payload = this.decodePayload(token);
    if (!payload?.exp) return true;
    return Date.now() >= payload.exp * 1000;
  }

  scheduleAutoLogout() {
    const token = this.getToken();
    if (!token) return;
    const payload = this.decodePayload(token);
    if (!payload?.exp) return;

    const msLeft = payload.exp * 1000 - Date.now();
    if (this.logoutTimer) clearTimeout(this.logoutTimer);
    if (msLeft > 0) {
      this.logoutTimer = setTimeout(() => this.logoutAndRedirect(), msLeft + 1000);
    } else {
      this.logoutAndRedirect();
    }
  }

  currentUserEmail(): string | null {
    const token = this.getToken();
    const payload = token ? this.decodePayload(token) : null;
    return payload?.email ?? null;
  }

  currentUserId(): string | null {
    const token = this.getToken();
    const payload = token ? this.decodePayload(token) : null;
    const nameIdUri = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';

    return (
      payload[nameIdUri] ?? // ASP.NET ClaimTypes.NameIdentifier as URI
      null
    );
  }

  private decodePayload(token: string): any | null {
    try {
      const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
      return JSON.parse(atob(base64));
    } catch {
      return null;
    }
  }
}
