import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { AuditLog } from '../models/audit.models';
import { PagedResult, ApiResponse } from '../models/user.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/audit-logs`;

  getLogs(page = 1, pageSize = 10, actions?: string[], severities?: string[], search?: string, sortBy?: string, sortDir?: string): Observable<PagedResult<AuditLog>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    actions?.forEach(a   => params = params.append('action',   a));
    severities?.forEach(s => params = params.append('severity', s));
    if (search)  params = params.set('search', search);
    if (sortBy)  params = params.set('sortBy', sortBy);
    if (sortDir) params = params.set('sortDir', sortDir);
    return this.http.get<ApiResponse<PagedResult<AuditLog>>>(this.base, { params }).pipe(map(r => r.data));
  }
}
