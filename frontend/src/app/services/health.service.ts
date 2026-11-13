import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of } from 'rxjs';
import { environment } from '../../environments/environment';

interface HealthResponse {
  status: string;
}

@Injectable({ providedIn: 'root' })
export class HealthService {
  private readonly http = inject(HttpClient);

  isOnline(): Observable<boolean> {
    return this.http
      .get<HealthResponse>(`${environment.apiBaseUrl}/health`, { observe: 'response' })
      .pipe(
        map((response) => response.status === 200 && response.body?.status === 'ok'),
        catchError(() => of(false)),
      );
  }
}
