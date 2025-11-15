import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';

// ⬅️ استيراد الـ Service والـ Interfaces من ملف الخدمة
import { 
  EmployeesService, 
  Employee, 
  Department, 
  EmployeeCreateRequest, 
  EmployeeUpdateRequest, 
  EmployeeStatus 
} from '../../services/employees.service'; 


@Component({
  selector: 'app-employees-management',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe], 
  templateUrl: './employees-management.component.html',
  styleUrl: './employees-management.component.css'
})
export class EmployeesManagementComponent implements OnInit {
  employees: Employee[] = [];
  departments: Department[] = [];
  statusOptions: EmployeeStatus[] = ['Active', 'Suspended', 'Terminated']; 

  loading = false;
  saving = false;
  error = '';
  showAddForm = false;
  isEditMode = false; 

  // النموذج الذي يمثل بيانات الطلب (مع إضافة حقل id مؤقت للتعديل)
  newEmployeeRequest: EmployeeCreateRequest & { id?: string } = this.getEmptyEmployeeRequest();
  
  // حقن الـ EmployeesService
  constructor(private readonly employeeService: EmployeesService) {} 

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.loading = true;
    this.error = '';

    // تحميل الأقسام
    this.employeeService.getDepartments().subscribe(departments => {
        this.departments = departments;
    });

    // تحميل الموظفين
    this.employeeService.getEmployees().subscribe({
        next: (employees) => {
            this.employees = employees;
            this.loading = false;
        },
        error: (err) => {
            console.error('Error loading employees:', err);
            this.error = 'Unable to load employee data.';
            this.loading = false;
        }
    });
  }

  saveEmployee() {
    // تحقق بسيط من الحقول الإلزامية
    if (!this.newEmployeeRequest.fullName || !this.newEmployeeRequest.employeeNumber || !this.newEmployeeRequest.email || !this.newEmployeeRequest.departmentId) {
        this.error = 'Please fill all required fields: Full Name, Employee Number, Email, and Department.';
        return;
    }

    this.saving = true;
    this.error = '';
    
    // فصل الـ id عن الـ payload (الـ id يستخدم في المسار فقط)
    const { id, initialPassword, ...payload } = this.newEmployeeRequest;
    
    let operation: Observable<any>;

    if (this.isEditMode && id) {
        // حالة التعديل تستخدم EmployeeUpdateRequest (لا تحتاج initialPassword)
        const updatePayload: EmployeeUpdateRequest = payload;
        operation = this.employeeService.updateEmployee(id, updatePayload);
    } else {
        // حالة الإنشاء تستخدم EmployeeCreateRequest
        const createPayload: EmployeeCreateRequest = { ...payload, initialPassword };
        operation = this.employeeService.createEmployee(createPayload);
    }

    operation.subscribe({
        next: () => {
            this.resetForm();
            this.loadData();
            this.showAddForm = false; 
        },
        error: (err) => {
            console.error('Error saving employee:', err);
            this.error = `Unable to ${this.isEditMode ? 'update' : 'add'} employee.`;
            this.saving = false;
        }
    });
  }

  // ⬅️ تم التعديل ليقبل ID مباشرةً
  deleteEmployee(id: string) {
    if (!confirm(`Are you sure you want to delete this employee?`)) {
      return;
    }

    this.employeeService.deleteEmployee(id).subscribe({
      next: () => {
        // إذا كان الموظف الذي تم حذفه هو نفسه المعروض في النموذج، قم بمسح النموذج
        if (this.newEmployeeRequest.id === id) {
            this.resetForm();
        }
        this.loadData();
      },
      error: (err) => {
        console.error('Error deleting employee:', err);
        this.error = 'Unable to delete employee.';
      }
    });
  }

  toggleForm() {
    this.showAddForm = !this.showAddForm;
    if (!this.showAddForm) {
      this.resetForm();
    }
  }

  selectEmployee(employee: Employee) {
    // بناء كائن الـ Request من بيانات الموظف المختار للتعديل
    this.newEmployeeRequest = {
        id: employee.id, // حقل الـ ID لإرساله في الـ PUT
        employeeNumber: employee.employeeNumber,
        fullName: employee.fullName,
        email: employee.email,
        departmentId: employee.departmentId,
        managerId: employee.managerId,
        birthDate: employee.birthDate,
        joinDate: employee.joinDate,
        jobTitle: employee.jobTitle,
        phoneNumber: employee.phoneNumber,
        notes: employee.notes,
        status: employee.status,
        initialPassword: null // يجب ألا نرسل كلمة مرور في وضع التعديل
    } as EmployeeCreateRequest & { id?: string };
    
    this.isEditMode = true;
    this.showAddForm = true; 
  }

  resetForm() {
    this.newEmployeeRequest = this.getEmptyEmployeeRequest();
    this.isEditMode = false;
    this.saving = false;
    this.error = '';
  }

  private getEmptyEmployeeRequest(): EmployeeCreateRequest & { id?: string } {
      return {
          id: undefined,
          employeeNumber: '',
          fullName: '',
          email: '',
          departmentId: '', 
          managerId: null,
          birthDate: null,
          joinDate: null,
          jobTitle: null,
          phoneNumber: null,
          notes: null,
          status: 'Active',
          initialPassword: null 
      };
  }
  
  trackByEmployeeId(_index: number, employee: Employee) {
    return employee.id;
  }
}