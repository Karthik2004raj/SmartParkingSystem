import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class ParkingService {
  private apiUrl = environment.apiUrl;
  
  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {
    console.log('ParkingService using API URL:', this.apiUrl);
  }
  
  private getHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    });
  }
  
  getDashboardStats(): Observable<any> {
    console.log('Fetching dashboard stats from:', `${this.apiUrl}/parking/dashboard`);
    return this.http.get(`${this.apiUrl}/parking/dashboard`, {
      headers: this.getHeaders()
    });
  }
  
  getAllSlots(): Observable<any> {
    console.log('Fetching all slots from:', `${this.apiUrl}/parking/slots`);
    return this.http.get(`${this.apiUrl}/parking/slots`, {
      headers: this.getHeaders()
    });
  }
  
  vehicleEntry(entry: any): Observable<any> {
    console.log('Vehicle entry to:', `${this.apiUrl}/parking/entry`);
    return this.http.post(`${this.apiUrl}/parking/entry`, entry, {
      headers: this.getHeaders()
    });
  }
  
  vehicleExit(slotId: number): Observable<any> {
    console.log('Vehicle exit from:', `${this.apiUrl}/parking/exit/${slotId}`);
    return this.http.post(`${this.apiUrl}/parking/exit/${slotId}`, {}, {
      headers: this.getHeaders()
    });
  }
}
