import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = environment.apiUrl;
  
  constructor(private http: HttpClient) {
    console.log('AuthService using API URL:', this.apiUrl);
  }
  
  login(username: string, password: string): Observable<any> {
    console.log('Login request to:', `${this.apiUrl}/auth/login`);
    return this.http.post(`${this.apiUrl}/auth/login`, { username, password })
      .pipe(
        tap((response: any) => {
          console.log('Login response received');
          if (response.token) {
            localStorage.setItem('token', response.token);
            localStorage.setItem('username', response.username);
            localStorage.setItem('role', response.role);
            localStorage.setItem('userId', response.userId);
          }
        })
      );
  }
  
  logout(): void {
    localStorage.clear();
  }
  
  isLoggedIn(): boolean {
    return !!localStorage.getItem('token');
  }
  
  getToken(): string | null {
    return localStorage.getItem('token');
  }
  
  getUserRole(): string | null {
    return localStorage.getItem('role');
  }
  
  getUsername(): string | null {
    return localStorage.getItem('username');
  }
}
