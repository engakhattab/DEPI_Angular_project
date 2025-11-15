import { AsyncPipe, NgIf, NgFor } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService, EmployeeProfile } from './core/auth/auth.service';

type NavLink = {
  path: string;
  label: string;
  icon?: string;
};

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, AsyncPipe, NgIf, NgFor],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly links: NavLink[] = [
    { path: '/home', label: 'Home' },
    { path: '/employees', label: 'Employees' },
    { path: '/vacation', label: 'Vacation' },
    { path: '/transportation', label: 'Transportation' }
  ];

  readonly employee$ = this.auth.employee$;

  async logout(): Promise<void> {
    this.auth.logout();
    await this.router.navigate(['/login']);
  }

  trackByPath(_index: number, link: NavLink): string {
    return link.path;
  }
}
