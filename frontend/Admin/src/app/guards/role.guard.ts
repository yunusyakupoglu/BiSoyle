import { inject } from '@angular/core';
import { Router, type CanActivateFn, RedirectCommand, UrlTree } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Role-based guard factory
 * Kullanım: canActivate: [roleGuard(['Admin', 'SuperAdmin'])]
 */
export function roleGuard(allowedRoles: string[]): CanActivateFn {
  return () => {
    const authService = inject(AuthService);
    const router: Router = inject(Router);

    // Kullanıcı giriş yapmış mı?
    if (!authService.isAuthenticated()) {
      const urlTree: UrlTree = router.parseUrl('/auth/sign-in');
      return new RedirectCommand(urlTree, { skipLocationChange: false });
    }

    // Kullanıcının rolleri
    const user = authService.getUser();
    const userRoles = user?.roles || [];

    // Kullanıcının herhangi bir rolü allowed roles'de var mı?
    const hasAccess = userRoles.some(role => allowedRoles.includes(role));

    if (hasAccess) {
      return true;
    }

    // Yetkisiz erişim - 403 veya dashboard'a yönlendir
    const urlTree: UrlTree = router.parseUrl('/error-404');
    return new RedirectCommand(urlTree, { skipLocationChange: false });
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





