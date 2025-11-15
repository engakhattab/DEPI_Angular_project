import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { VacationRequest, VacationRequestsService, VacationStatus } from '../../services/vacation-requests.service';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-vacation',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  templateUrl: './vacation.component.html',
  styleUrl: './vacation.component.css'
})
export class VacationComponent implements OnInit {
  showAddForm = false;
  requests: VacationRequest[] = [];
  loading = false;
  saving = false;
  error = '';
  filter: VacationStatus | 'All' = 'All';

  newRequest = {
    startDate: '',
    endDate: '',
    reason: ''
  };

  readonly statusOptions: VacationStatus[] = ['Pending', 'Approved', 'Rejected'];

  constructor(
    private readonly vacationRequestsService: VacationRequestsService,
    private readonly authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadRequests();
  }

  loadRequests(): void {
    this.loading = true;
    this.error = '';
    const options =
      this.filter === 'All'
        ? {}
        : {
            status: this.filter
          };
    this.vacationRequestsService.getRequests(options).subscribe({
      next: (requests) => {
        this.requests = requests;
        this.loading = false;
      },
      error: (err) => {
        console.error('Failed to load vacation requests', err);
        this.error = 'Unable to load vacation requests.';
        this.loading = false;
      }
    });
  }

  addVacationRequest(): void {
    if (!this.newRequest.startDate || !this.newRequest.endDate || !this.newRequest.reason) {
      return;
    }

    const employee = this.authService.employee();
    if (!employee) {
      this.error = 'Please sign in again to submit requests.';
      return;
    }

    this.saving = true;
    this.vacationRequestsService
      .createRequest({
        employeeId: employee.id,
        startDate: this.newRequest.startDate,
        endDate: this.newRequest.endDate,
        reason: this.newRequest.reason
      })
      .subscribe({
        next: () => {
          this.resetForm();
          this.loadRequests();
        },
        error: (err) => {
          console.error('Failed to create vacation request', err);
          this.error = 'Unable to create vacation request.';
          this.saving = false;
        }
      });
  }

  updateStatus(request: VacationRequest, status: VacationStatus): void {
    this.vacationRequestsService.updateStatus(request.id, { status }).subscribe({
      next: () => this.loadRequests(),
      error: (err) => {
        console.error('Failed to update vacation status', err);
        this.error = 'Unable to update vacation status.';
      }
    });
  }

  deleteRequest(request: VacationRequest): void {
    if (!confirm('Delete this vacation request?')) {
      return;
    }

    this.vacationRequestsService.deleteRequest(request.id).subscribe({
      next: () => this.loadRequests(),
      error: (err) => {
        console.error('Failed to delete vacation request', err);
        this.error = 'Unable to delete vacation request.';
      }
    });
  }

  setFilter(status: VacationStatus | 'All'): void {
    this.filter = status;
    this.loadRequests();
  }

  private resetForm(): void {
    this.newRequest = {
      startDate: '',
      endDate: '',
      reason: ''
    };
    this.showAddForm = false;
    this.saving = false;
  }
}
