import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

@Injectable()
export class JwtInterceptor implements HttpInterceptor {
  constructor(private auth: AuthService) { }

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    let request = req;
    const token = this.auth.getToken();

    // Only attach if present and not expired
    if (token && !this.auth.isTokenExpired()) {
      request = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
    }

    return next.handle(request).pipe(
      catchError((err: HttpErrorResponse) => {
        if (err.status === 401) {
          // Optional: if server sends Token-Expired header, you could check it here
          this.auth.logoutAndRedirect();
        }
        return throwError(() => err);
      })
    );
  }
}
