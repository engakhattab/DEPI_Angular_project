import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export type VacationStatus = 'Pending' | 'Approved' | 'Rejected';

export type VacationRequest = {
  id: string;
  employeeId: string;
  employeeName: string;
  startDate: string;
  endDate: string;
  reason: string;
  status: VacationStatus;
  createdAt: string;
  updatedAt?: string | null;
};

export type VacationRequestCreateRequest = {
  employeeId: string;
  startDate: string;
  endDate: string;
  reason: string;
};

export type VacationRequestStatusUpdateRequest = {
  status: VacationStatus;
};

@Injectable({ providedIn: 'root' })
export class VacationRequestsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/VacationRequests`;

  getRequests(options?: { status?: VacationStatus; employeeId?: string }): Observable<VacationRequest[]> {
    let params = new HttpParams();
    if (options?.status) {
      params = params.set('status', options.status);
    }
    if (options?.employeeId) {
      params = params.set('employeeId', options.employeeId);
    }
    return this.http.get<VacationRequest[]>(this.baseUrl, { params });
  }

  createRequest(payload: VacationRequestCreateRequest): Observable<VacationRequest> {
    return this.http.post<VacationRequest>(this.baseUrl, payload);
  }

  updateStatus(id: string, payload: VacationRequestStatusUpdateRequest): Observable<VacationRequest> {
    return this.http.put<VacationRequest>(`${this.baseUrl}/${id}/status`, payload);
  }

  deleteRequest(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
