import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, throwError, firstValueFrom } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
}

export interface User {
  id: number;
  tenantId?: number | null;
  username: string;
  email: string;
  firstName?: string;
  lastName?: string;
  avatar?: string;
  location?: string;
  title?: string;
  roles?: string[];
}

export interface LoginResponse {
  user: User;
  token: string;
  refreshToken: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = environment.apiUrl;
  private readonly tokenKey = '_BISOYLE_AUTH_TOKEN_';
  private readonly userKey = '_BISOYLE_USER_DATA_';
  private readonly refreshTokenKey = '_BISOYLE_REFRESH_TOKEN_';
  private readonly globalLicenseKey = '_BISOYLE_LICENSE_VALID_';

  constructor(private http: HttpClient, private router: Router) {}

  private getLicenseFlagKey(user?: User | null): string {
    const resolvedUser = user ?? this.getUser();
    if (!resolvedUser || !resolvedUser.tenantId || resolvedUser.tenantId === 0) {
      return `${this.globalLicenseKey}_SUPER`;
    }
    return `${this.globalLicenseKey}_TENANT_${resolvedUser.tenantId}`;
  }

  private hasLicenseFlag(user?: User | null): boolean {
    const key = this.getLicenseFlagKey(user);
    return localStorage.getItem(key) === 'true';
  }

  markLicenseValidated(user?: User | null, tenantId?: number | null): void {
    if (user && (user.tenantId === null || user.tenantId === 0)) {
      return;
    }
    const resolvedUser = user ?? this.getUser();
    const flagUser: User | null = resolvedUser
      ? { ...resolvedUser, tenantId: tenantId ?? resolvedUser.tenantId }
      : tenantId
      ? ({ tenantId } as User)
      : resolvedUser;
    const key = this.getLicenseFlagKey(flagUser);
    localStorage.setItem(key, 'true');
  }

  clearLicenseValidation(user?: User | null): void {
    const key = this.getLicenseFlagKey(user);
    localStorage.removeItem(key);
  }

  async ensureLicenseFlagFromServer(): Promise<void> {
    const token = this.getToken();
    if (!token) return;
    try {
      // Always refresh user from server to ensure correct tenantId/roles
      let headers = new HttpHeaders({ 'Content-Type': 'application/json' });
      headers = headers.set('Authorization', `Bearer ${token}`);
      const me = await firstValueFrom(this.http.get<any>(`${this.apiUrl}/auth/me`, { headers }));
      if (!me) return;
      const refreshed: User = {
        id: me.id,
        tenantId: me.tenantId ?? me.TenantId ?? null,
        username: me.username,
        email: me.email,
        firstName: me.firstName,
        lastName: me.lastName,
        avatar: me.avatar,
        location: me.location,
        title: me.title,
        roles: me.roles
      };
      this.setUser(refreshed);
      const isSA = Array.isArray(refreshed.roles) && refreshed.roles.includes('SuperAdmin');
      if (isSA || !refreshed.tenantId) return;
      const tenant: any = await firstValueFrom(
        this.http.get(`${this.apiUrl}/tenants/${refreshed.tenantId}`, { headers })
      );
      if (tenant?.licenseKeyGirisTarihi) {
        this.markLicenseValidated(refreshed);
      }
    } catch (error) {
      console.warn('License status check failed', error);
    }
  }

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/auth/login`, credentials).pipe(
      tap(response => {
        if (response && response.token) {
          localStorage.setItem(this.tokenKey, response.token);
          localStorage.setItem(this.userKey, JSON.stringify(response.user));
          localStorage.setItem(this.refreshTokenKey, response.refreshToken);
          
          // Sunucudan güncel kullanıcı bilgisini çek (tenantId ve roller doğru olsun)
          try {
            let headers = new HttpHeaders({ 'Content-Type': 'application/json' });
            headers = headers.set('Authorization', `Bearer ${response.token}`);
            this.http.get<any>(`${this.apiUrl}/auth/me`, { headers }).subscribe({
              next: (me) => {
                if (me) {
                  localStorage.setItem(this.userKey, JSON.stringify({
                    id: me.id,
                    tenantId: me.tenantId ?? me.TenantId ?? null,
                    username: me.username,
                    email: me.email,
                    firstName: me.firstName,
                    lastName: me.lastName,
                    avatar: me.avatar,
                    location: me.location,
                    title: me.title,
                    roles: me.roles
                  } as User));
                }
              },
              error: () => {}
            });
          } catch {}
          
          // Eğer kullanıcı SuperAdmin değilse ve lisans anahtarı henüz doğrulanmamışsa
          // license-key sayfasına yönlendirilecek (login component'te kontrol edilecek)
          const user = response.user;
          if (user && user.tenantId && user.tenantId !== 0 && 
              !user.roles?.includes('SuperAdmin') &&
              !this.hasLicenseFlag(user)) {
            // License key kontrolü için flag set edilmediyse, login component'te yönlendirme yapılacak
          }
        }
      }),
      catchError(error => {
        console.error('Login error:', error);
        return throwError(() => error);
      })
    );
  }

  register(userData: RegisterRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/auth/register`, userData).pipe(
      catchError(error => {
        console.error('Register error:', error);
        return throwError(() => error);
      })
    );
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  getUser(): User | null {
    const userStr = localStorage.getItem(this.userKey);
    if (userStr) {
      return JSON.parse(userStr);
    }
    return null;
  }

  setUser(user: User): void {
    localStorage.setItem(this.userKey, JSON.stringify(user));
  }

  isAuthenticated(): boolean {
    return this.getToken() !== null;
  }

  /**
   * Kullanıcının lisans doğrulaması yapılmış mı kontrol eder
   * SuperAdmin her zaman true döner
   */
  isLicenseValidated(): boolean {
    // Geçici olarak test için her zaman true döndür
    if (localStorage.getItem('_BISOYLE_SKIP_LICENSE_CHECK_') === 'true') {
      return true;
    }
    
    const user = this.getUser();
    const isSuperAdmin = this.isSuperAdmin();

    if (isSuperAdmin) {
      return true; // SuperAdmin için lisans kontrolü gerekmez
    }

    return this.hasLicenseFlag(user);
  }

  /**
   * Kullanıcının SuperAdmin olup olmadığını kontrol eder
   */
  isSuperAdmin(): boolean {
    const user = this.getUser();
    if (!user) {
      return false;
    }
    return Array.isArray(user.roles) && user.roles.includes('SuperAdmin');
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
    localStorage.removeItem(this.refreshTokenKey);
    this.router.navigate(['/auth/sign-in']);
  }

  getMe(): Observable<User> {
    const token = this.getToken();
    let headers = new HttpHeaders();
    if (token) {
      headers = headers.set('Authorization', `Bearer ${token}`);
    }
    
    return this.http.get<User>(`${this.apiUrl}/auth/me`, { headers }).pipe(
      tap(user => this.setUser(user)),
      catchError(error => {
        console.error('Get user error:', error);
        // Only logout if it's a 401 (unauthorized), not for other errors
        if (error.status === 401) {
          this.logout();
        }
        return throwError(() => error);
      })
    );
  }

  getUsers(): Observable<User[]> {
    return this.http.get<User[]>(`${this.apiUrl}/users`);
  }
}


