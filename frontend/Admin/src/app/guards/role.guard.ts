import { inject } from '@angular/core';
import { Router, type CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Role-based guard factory
 * Kullanım: canActivate: [roleGuard(['Admin', 'SuperAdmin'])]
 * Not: Bu guard lisans kontrolü de yapar (SuperAdmin hariç)
 */
export function roleGuard(allowedRoles: string[]): CanActivateFn {
  return (route, state) => {
    const authService = inject(AuthService);
    const router: Router = inject(Router);

    const check = async () => {
      if (!authService.isAuthenticated()) {
        await router.navigate(['/auth/login'], { queryParams: { returnUrl: state.url } });
        return false;
      }

      const user = authService.getUser();
      const userRoles = user?.roles || [];
      const hasAccess = userRoles.some(role => allowedRoles.includes(role));

      if (!hasAccess) {
        await router.navigate(['/error-404']);
        return false;
      }

      if (authService.isSuperAdmin()) {
        return true;
      }

      await authService.ensureLicenseFlagFromServer();

      if (!authService.isLicenseValidated()) {
        await router.navigate(['/license-activation'], { queryParams: { returnUrl: state.url } });
        return false;
      }

      return true;
    };

    return check();
  };
}

/**
 * SuperAdmin-only guard
 */
export const superAdminGuard: CanActivateFn = roleGuard(['SuperAdmin']);

/**
 * Admin (firm admin) guard
 */
export const adminGuard: CanActivateFn = roleGuard(['Admin', 'SuperAdmin']);

/**
 * User (any authenticated user) guard
 */
export const userGuard: CanActivateFn = roleGuard(['User', 'Admin', 'SuperAdmin']);










