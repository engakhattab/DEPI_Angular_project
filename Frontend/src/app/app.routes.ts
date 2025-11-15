import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { HomeComponent } from './components/home/home.component';
import { EmployeesManagementComponent} from './components/employees-management/employees-management.component';
import { VacationComponent } from './components/vacation/vacation.component';
import { TransportationComponent } from './components/transportation/transportation.component';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'home', canActivate: [authGuard], component: HomeComponent },
  { path: 'employees', canActivate: [authGuard], component: EmployeesManagementComponent },
  { path: 'vacation', canActivate: [authGuard], component: VacationComponent },
  { path: 'transportation', canActivate: [authGuard], component: TransportationComponent },
  { path: '', pathMatch: 'full', redirectTo: 'home' },
  { path: '**', redirectTo: 'home' }
];
