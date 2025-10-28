import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';

export interface LoginRequest {
  kullanici_adi: string;
  parola: string;
}

export interface RegisterRequest {
  ad: string;
  soyad: string;
  kullanici_adi: string;
  parola: string;
  rol_id: number;
}

export interface TokenResponse {
  access_token: string;
  token_type: string;
}

export interface UserResponse {
  id: number;
  ad: string;
  soyad: string;
  kullanici_adi: string;
  rol_id: number;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = 'http://localhost:8000';
  private tokenKey = 'auth_token';
  private userKey = 'user_data';

  constructor(private http: HttpClient, private router: Router) {}

  login(credentials: LoginRequest): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${this.apiUrl}/auth/login`, credentials).pipe(
      tap(response => {
        if (response.access_token) {
          localStorage.setItem(this.tokenKey, response.access_token);
        }
      }),
      catchError(error => {
        console.error('Login error:', error);
        return throwError(() => error);
      })
    );
  }

  register(userData: RegisterRequest): Observable<UserResponse> {
    return this.http.post<UserResponse>(`${this.apiUrl}/auth/register`, userData).pipe(
      catchError(error => {
        console.error('Register error:', error);
        return throwError(() => error);
      })
    );
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  getUser(): UserResponse | null {
    const userStr = localStorage.getItem(this.userKey);
    if (userStr) {
      return JSON.parse(userStr);
    }
    return null;
  }

  setUser(user: UserResponse): void {
    localStorage.setItem(this.userKey, JSON.stringify(user));
  }

  isAuthenticated(): boolean {
    return this.getToken() !== null;
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
    this.router.navigate(['/auth/sign-in']);
  }

  getMe(): Observable<UserResponse> {
    return this.http.get<UserResponse>(`${this.apiUrl}/auth/me`).pipe(
      tap(user => this.setUser(user)),
      catchError(error => {
        console.error('Get user error:', error);
        this.logout();
        return throwError(() => error);
      })
    );
  }
}


