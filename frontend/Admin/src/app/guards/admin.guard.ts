import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const adminGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  
  // Kullanıcı giriş yapmış mı kontrol et
  if (!authService.isAuthenticated()) {
    router.navigate(['/auth/login']);
    return false;
  }
  
  // TODO: JWT token'dan rol bilgisini al ve admin kontrolü yap
  // Şimdilik sadece authenticated ise geç
  // İleride rol bilgisini de kontrol edebiliriz
  
  return true;
};



