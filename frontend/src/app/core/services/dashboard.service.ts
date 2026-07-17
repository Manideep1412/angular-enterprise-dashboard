import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/dashboard`;

  getStats(): Observable<any> {
    return this.http.get<any>(`${this.base}/stats`).pipe(
      map(r => r.data ?? r)
    );
  }
}
