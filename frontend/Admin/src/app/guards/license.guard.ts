import { inject } from '@angular/core';
import { Router, type CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * License Guard - SuperAdmin hariç tüm kullanıcılar için lisans kontrolü yapar
 * Lisans doğrulanmamışsa license-activation sayfasına yönlendirir
 */
export const licenseGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router: Router = inject(Router);

  const check = async () => {
    if (!authService.isAuthenticated()) {
      await router.navigate(['/auth/login'], { queryParams: { returnUrl: state.url } });
      return false;
    }

    if (authService.isSuperAdmin()) {
      return true;
    }

    // Önce yerel lisans bayrağını kontrol et (aktivasyondan hemen sonra gecikmesiz geçiş için)
    if (authService.isLicenseValidated()) {
      // Eğer zaten license-activation sayfasındaysa, doğrudan dashboard'a taşı
      if (state.url.startsWith('/license-activation')) {
        await router.navigate(['/dashboard']);
        return false;
      }
      return true;
    }

    // Sunucudan teyit et (gecikmeli durumlar için)
    await authService.ensureLicenseFlagFromServer();
    if (state.url.startsWith('/license-activation')) {
      return true;
    }

    if (!authService.isLicenseValidated()) {
      await router.navigate(['/license-activation'], { queryParams: { returnUrl: state.url } });
      return false;
    }

    return true;
  };

  return check();
};


