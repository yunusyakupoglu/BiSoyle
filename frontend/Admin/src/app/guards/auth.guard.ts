import { Injectable } from '@angular/core';
import { Router, CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  async canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Promise<boolean> {
    if (!this.authService.isAuthenticated()) {
      await this.router.navigate(['/auth/login'], { queryParams: { returnUrl: state.url } });
      return false;
    }

    if (this.authService.isSuperAdmin()) {
      return true;
    }

    // Aktivasyondan hemen sonra yerel bayrağı önceliklendir
    if (this.authService.isLicenseValidated()) {
      if (state.url.startsWith('/license-activation')) {
        await this.router.navigate(['/dashboard']);
        return false;
      }
      return true;
    }

    // Sunucu teyidi (gecikmeli durumlar için)
    await this.authService.ensureLicenseFlagFromServer();

    if (state.url.startsWith('/license-activation')) {
      return true;
    }

    if (!this.authService.isLicenseValidated()) {
      await this.router.navigate(['/license-activation'], { queryParams: { returnUrl: state.url } });
      return false;
    }

    return true;
  }
}




