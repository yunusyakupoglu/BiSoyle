import { Injectable, inject } from '@angular/core'
import { HttpClient, HttpHeaders } from '@angular/common/http'
import { map } from 'rxjs/operators'
import { Observable } from 'rxjs'
import { CookieService } from 'ngx-cookie-service'
import { environment } from '../../../environments/environment'

export interface User {
  id?: number
  username?: string
  email?: string
  firstName?: string
  lastName?: string
  avatar?: string
  location?: string
  title?: string
  roles?: string[]
  token?: string
}

export interface LoginResponse {
  intface?: any
  user: User
  token: string
  refreshToken: string
}

@Injectable({ providedIn: 'root' })
export class AuthenticationService {
  user: User | null = null
  private apiDto: any
  private apiUrl = environment.apiUrl

  public readonly authSessionKey = '_BISOYLE_AUTH_TOKEN_'
  public readonly userDataKey = '_BISOYLE_USER_DATA_'
  private cookieService = inject(CookieService)

  constructor(private http: HttpClient) {
    // Restore user from localStorage
    const userData = localStorage.getItem(this.userDataKey)
    if (userData) {
      this.user = JSON.parse(userData)
    }
  }

  login(email: string, password: string): Observable<User> {
    const payload = { email: (email || '').trim(), password }
    return this.http.post<LoginResponse>(`${this.apiUrl}/auth/login`, payload).pipe(
      map((response) => {
        if (response && response.token) {
          const user = { ...response.user, token: response.token }
          this.user = user
          this.saveSession(response.token)
          localStorage.setItem(this.userDataKey, JSON.stringify(user))
          localStorage.setItem('_BISOYLE_REFRESH_TOKEN_', response.refreshToken)
          // Fetch fresh user (ensures correct tenantId/roles)
          const headers = new HttpHeaders({ Authorization: `Bearer ${response.token}` })
          this.http.get<User>(`${this.apiUrl}/auth/me`, { headers }).subscribe({
            next: (fresh) => {
              const merged = { ...user, ...fresh }
              localStorage.setItem(this.userDataKey, JSON.stringify(merged))
            },
          })
        }
        return response.user
      })
    )
  }

  register(username: string, email: string, password: string, firstName?: string, lastName?: string): Promise<any> {
    return this.http.post(`${this.apiUrl}/auth/register`, {
      username,
      email,
      password,
      firstName,
      lastName
    }).toPromise()
  }

  getCurrentUser(): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/auth/me`)
  }

  logout(): void {
    // remove user from storage
    this.removeSession()
    localStorage.removeItem(this.userDataKey)
    localStorage.removeItem('_BISOYLE_REFRESH_TOKEN_')
    this.user = null
  }

  get session(): string {
    return this.cookieService.get(this.authSessionKey)
  }

  saveSession(token: string): void {
    this.cookieService.set(this.authSessionKey, token)
  }

  removeSession(): void {
    this.cookieService.delete(this.authSessionKey)
  }
}
