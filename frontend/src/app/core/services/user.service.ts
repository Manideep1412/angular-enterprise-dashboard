import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { User, PagedResult, CreateUserRequest, UpdateUserRequest, ApiResponse } from '../models/user.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/users`;

  getUsers(page = 1, pageSize = 10, search?: string, statuses?: string[], roles?: string[], sortBy?: string, sortDir?: string): Observable<PagedResult<User>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search)  params = params.set('search', search);
    statuses?.forEach(s => params = params.append('status', s));
    roles?.forEach(r => params = params.append('role', r));
    if (sortBy)  params = params.set('sortBy', sortBy);
    if (sortDir) params = params.set('sortDir', sortDir);
    return this.http.get<ApiResponse<PagedResult<User>>>(this.base, { params }).pipe(map(r => r.data));
  }

  getById(id: number): Observable<User> {
    return this.http.get<ApiResponse<User>>(`${this.base}/${id}`).pipe(map(r => r.data));
  }

  create(req: CreateUserRequest): Observable<User> {
    return this.http.post<ApiResponse<User>>(this.base, req).pipe(map(r => r.data));
  }

  update(id: number, req: UpdateUserRequest): Observable<User> {
    return this.http.put<ApiResponse<User>>(`${this.base}/${id}`, req).pipe(map(r => r.data));
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
