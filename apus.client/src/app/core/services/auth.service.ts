import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs/operators';
import { BehaviorSubject, Observable } from 'rxjs';
import { JwtResponse, LoginDto, RegisterDto } from '../../features/auth/Dto/AuthDtos';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private baseUrl = 'https://localhost:54954/api/auth';
  private _loggedIn$ = new BehaviorSubject<boolean>(!!this.getToken());
  public loggedIn$: Observable<boolean> = this._loggedIn$.asObservable();
  constructor(private http: HttpClient) { }

  register(dto: RegisterDto): Observable<any> {
    return this.http.post(`${this.baseUrl}/register`, dto);
  }

  login(dto: LoginDto): Observable<JwtResponse> {
    return this.http.post<JwtResponse>(`${this.baseUrl}/login`, dto)
      .pipe(
        tap(res => {
          // store the token…
          localStorage.setItem('jwt', res.token);
          // …and notify subscribers that we’re now logged in
          this._loggedIn$.next(true);
        })
      );
  }

  logout() {
    localStorage.removeItem('jwt');
    this._loggedIn$.next(false);
  }

  getToken(): string | null {
    return localStorage.getItem('jwt');
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }


}
