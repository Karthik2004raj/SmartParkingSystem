import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html'
})
export class LoginComponent {
  username = '';
  password = '';
  isLoading = false;
  errorMessage = '';
  
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}
  
  onSubmit(): void {
    this.isLoading = true;
    this.errorMessage = '';
    
    this.authService.login(this.username, this.password).subscribe({
      next: () => {
        this.router.navigate(['/dashboard']);
      },
      error: (error: any) => {
        this.errorMessage = error.error?.message || 'Login failed';
        this.isLoading = false;
      }
    });
  }
}
