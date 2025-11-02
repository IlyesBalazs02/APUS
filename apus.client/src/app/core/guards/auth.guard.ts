import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, CanActivateChild, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate, CanActivateChild {
  constructor(private auth: AuthService, private router: Router) { }

  private check(): boolean | UrlTree {
    // No token or expired -> redirect
    if (!this.auth.getToken() || this.auth.isTokenExpired()) {
      this.auth.logoutAndRedirect();
      return false;
    }
    // ensure auto-logout is armed after hard refresh
    this.auth.scheduleAutoLogout();
    return true;
  }

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean | UrlTree {
    return this.check();
  }
  canActivateChild(childRoute: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean | UrlTree {
    return this.check();
  }
}
