import { Injectable, inject } from '@angular/core'
import { HttpClient } from '@angular/common/http'
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
  user: User
  token: string
  refreshToken: string
}

@Injectable({ providedIn: 'root' })
export class AuthenticationService {
  user: User | null = null
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
    return this.http.post<LoginResponse>(`${this.apiUrl}/auth/login`, { email, password }).pipe(
      map((response) => {
        // login successful if there's a jwt token in the response
        if (response && response.token) {
          const user = { ...response.user, token: response.token }
          this.user = user
          // store user details and jwt token
          this.saveSession(response.token)
          localStorage.setItem(this.userDataKey, JSON.stringify(user))
          localStorage.setItem('_BISOYLE_REFRESH_TOKEN_', response.refreshToken)
        }
        return response.user
      })
    )
  }

  register(username: string, email: string, password: string, firstName?: string, lastName?: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/auth/register`, {
      username,
      email,
      password,
      firstName,
      lastName
    })
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
