import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, tap, BehaviorSubject } from 'rxjs';
import { environment } from '../../../environments/environment';

export type LoginCredentials = {
  identifier: string;
  password: string;
};

export type EmployeeProfile = {
  id: string;
  employeeNumber: string;
  fullName: string;
  email: string;
  departmentId: string;
  departmentName: string;
  managerId?: string | null;
  managerName?: string | null;
  birthDate?: string | null;
  joinDate?: string | null;
  jobTitle?: string | null;
  phoneNumber?: string | null;
  notes?: string | null;
  status?: string | null;
  identityUserId: string;
  userName: string;
};

type LoginResponse = { employee: EmployeeProfile };

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly storageKey = 'auth_employee';

  private readonly employeeSubject = new BehaviorSubject<EmployeeProfile | null>(this.hydrateEmployee());
  readonly employee$ = this.employeeSubject.asObservable();

  private get storage(): Storage | null {
    try {
      return typeof window !== 'undefined' ? window.localStorage : null;
    } catch {
      return null;
    }
  }

  isLoggedIn(): boolean {
    return !!this.employeeSubject.value;
  }

  employee(): EmployeeProfile | null {
    return this.employeeSubject.value;
  }

  login(credentials: LoginCredentials): Observable<EmployeeProfile> {
    const url = `${environment.apiBaseUrl}/Auth/login`;
    return this.http.post<LoginResponse>(url, credentials).pipe(
      map((response) => {
        if (!response?.employee) {
          throw new Error('Malformed login response');
        }
        return response.employee;
      }),
      tap((employee) => {
        this.persistEmployee(employee);
        this.employeeSubject.next(employee);
      })
    );
  }

  logout(): void {
    const storage = this.storage;
    if (storage) {
      storage.removeItem(this.storageKey);
    }
    this.employeeSubject.next(null);
  }

  private hydrateEmployee(): EmployeeProfile | null {
    const storage = this.storage;
    if (!storage) {
      return null;
    }

    const raw = storage.getItem(this.storageKey);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as EmployeeProfile;
    } catch {
      storage.removeItem(this.storageKey);
      return null;
    }
  }

  private persistEmployee(employee: EmployeeProfile): void {
    const storage = this.storage;
    if (!storage) {
      return;
    }
    storage.setItem(this.storageKey, JSON.stringify(employee));
  }
}
