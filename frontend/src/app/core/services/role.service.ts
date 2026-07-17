import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Role } from '../models/role.models';
import { ApiResponse } from '../models/user.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class RoleService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/roles`;

  getRoles(): Observable<Role[]> {
    return this.http.get<ApiResponse<Role[]>>(this.base).pipe(map(r => r.data));
  }
}
