import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService, LoginCredentials } from '../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {
  loading = false;
  error = '';

  private readonly fb = inject(FormBuilder);

  form = this.fb.group({
    identifier: ['', Validators.required],
    password: ['', Validators.required]
  });

  constructor(
    private auth: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    if (this.auth.isLoggedIn()) {
      const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/home';
      this.router.navigateByUrl(returnUrl);
    }
  }

  onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading = true;
    this.error = '';

    const credentials = this.form.getRawValue() as LoginCredentials;

    // Frontend note: AuthService expects the camelCase login payload produced by the HR API (Program.cs).
    this.auth.login(credentials).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/home';
        this.router.navigateByUrl(returnUrl);
        this.form.reset({ identifier: '', password: '' });
        this.loading = false;
      },
      error: (err) => {
        const backendMessage =
          typeof err?.error === 'string'
            ? err.error
            : err?.error?.title || err?.error?.message;
        this.error = backendMessage || 'Invalid credentials';
        this.loading = false;
      }
    });
  }
}
