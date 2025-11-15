import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export type EmployeeStatus = 'Active' | 'Suspended' | 'Terminated';

export type Department = {
  id: string;
  name: string;
};

export type Employee = {
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
  status: EmployeeStatus;
  identityUserId: string;
  userName: string;
};

export type EmployeeCreateRequest = {
  employeeNumber: string;
  fullName: string;
  email: string;
  departmentId: string;
  managerId?: string | null;
  birthDate?: string | null;
  joinDate?: string | null;
  jobTitle?: string | null;
  phoneNumber?: string | null;
  notes?: string | null;
  status: EmployeeStatus;
  initialPassword?: string | null;
};

export type EmployeeUpdateRequest = Omit<EmployeeCreateRequest, 'initialPassword'>;

export type EmployeeCreatedResponse = {
  employee: Employee;
  temporaryPassword?: string | null;
};

@Injectable({ providedIn: 'root' })
export class EmployeesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/Employees`;
  private readonly departmentsUrl = `${environment.apiBaseUrl}/Departments`;

  getEmployees(status?: EmployeeStatus): Observable<Employee[]> {
    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }
    return this.http.get<Employee[]>(this.baseUrl, { params });
  }

  getDepartments(): Observable<Department[]> {
    return this.http.get<Department[]>(this.departmentsUrl);
  }

  createEmployee(payload: EmployeeCreateRequest): Observable<EmployeeCreatedResponse> {
    return this.http.post<EmployeeCreatedResponse>(this.baseUrl, payload);
  }

  updateEmployee(id: string, payload: EmployeeUpdateRequest): Observable<Employee> {
    return this.http.put<Employee>(`${this.baseUrl}/${id}`, payload);
  }

  deleteEmployee(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
