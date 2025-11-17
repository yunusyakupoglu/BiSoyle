import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private readonly authSessionKey = '_BISOYLE_AUTH_TOKEN_';

  constructor(private router: Router) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Token'ı localStorage'dan al (çeşitli anahtar adları için geriye dönük uyumluluk)
    const token =
      localStorage.getItem(this.authSessionKey) ||
      localStorage.getItem('_BISOYLE_AUTH_TOKEN') ||
      localStorage.getItem('auth_token') ||
      localStorage.getItem('token');
    
    if (token) {
      req = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }
    
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        // 401 Unauthorized - Token expired or invalid
        if (error.status === 401) {
          // Clear authentication data
          localStorage.removeItem(this.authSessionKey);
          localStorage.removeItem('_BISOYLE_AUTH_TOKEN');
          localStorage.removeItem('auth_token');
          localStorage.removeItem('token');
          localStorage.removeItem('_BISOYLE_USER_DATA_');
          localStorage.removeItem('_BISOYLE_REFRESH_TOKEN_');
          
          // Only redirect if not already on login page
          if (!this.router.url.includes('/auth/')) {
            this.router.navigate(['/auth/login'], { 
              queryParams: { returnUrl: this.router.url } 
            });
          }
        }
        
        return throwError(() => error);
      })
    );
  }
}




