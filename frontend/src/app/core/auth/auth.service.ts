import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap, catchError, EMPTY } from 'rxjs';
import { environment } from '@env/environment';
import { LoginRequest, LoginResponse, UserInfo } from '../models/auth.models';

const TOKEN_KEY = 'ed_access_token';
const USER_KEY = 'ed_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  // ── Signals ──────────────────────────────────────────────────────────────
  private readonly _token = signal<string | null>(this.loadToken());
  private readonly _user = signal<UserInfo | null>(this.loadUser());
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  // ── Public readonly signals ───────────────────────────────────────────────
  readonly token = this._token.asReadonly();
  readonly user = this._user.asReadonly();
  readonly isLoading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly isAuthenticated = computed(() => !!this._token() && !!this._user());
  readonly currentRoles = computed(() => this._user()?.roles ?? []);
  readonly isAdmin = computed(() => this.currentRoles().includes('Admin'));
  readonly isManagerOrAdmin = computed(() =>
    this.currentRoles().some(r => ['Admin', 'Manager'].includes(r))
  );

  login(request: LoginRequest) {
    this._loading.set(true);
    this._error.set(null);

    return this.http.post<{ success: boolean; data: LoginResponse }>(
      `${environment.apiUrl}/api/v1/auth/login`, request
    ).pipe(
      tap(response => {
        if (response.success) {
          this.storeAuth(response.data.accessToken, response.data.user);
          this.router.navigate(['/dashboard']);
        }
      }),
      catchError(err => {
        this._error.set(err.error?.message ?? 'Invalid email or password');
        this._loading.set(false);
        return EMPTY;
      }),
      tap(() => this._loading.set(false))
    );
  }

  logout() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this._token.set(null);
    this._user.set(null);
    this.router.navigate(['/login']);
  }

  private storeAuth(token: string, user: UserInfo) {
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this._token.set(token);
    this._user.set(user);
  }

  private loadToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  private loadUser(): UserInfo | null {
    const stored = localStorage.getItem(USER_KEY);
    return stored ? JSON.parse(stored) : null;
  }

  hasRole(role: string): boolean {
    return this.currentRoles().includes(role);
  }
}
