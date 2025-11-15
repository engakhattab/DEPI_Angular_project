import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export type Trip = {
  id: string;
  referenceName: string;
  project: string;
  route: string;
  tripType: string;
  tripDate: string;
  tripCode: string;
  requestCode: string;
  createdAt: string;
};

export type TripCreateRequest = {
  referenceName: string;
  project: string;
  route: string;
  tripType: string;
  tripDate: string;
};

@Injectable({ providedIn: 'root' })
export class TransportationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/Trips`;

  getTrips(): Observable<Trip[]> {
    return this.http.get<Trip[]>(this.baseUrl);
  }

  createTrip(payload: TripCreateRequest): Observable<Trip> {
    return this.http.post<Trip>(this.baseUrl, payload);
  }

  deleteTrip(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
