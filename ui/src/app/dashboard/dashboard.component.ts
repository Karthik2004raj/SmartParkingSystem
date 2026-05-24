import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { ParkingService } from '../services/parking.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  stats: any = {
    totalSlots: 0,
    occupiedSlots: 0,
    availableSlots: 0,
    utilizationPercentage: 0,
    congestionLevel: 'Normal',
    predictedNextHourOccupancy: 0
  };
  username = '';
  role = '';
  
  constructor(
    private authService: AuthService,
    private parkingService: ParkingService,
    private router: Router
  ) {
    this.username = this.authService.getUsername() || '';
    this.role = this.authService.getUserRole() || '';
  }
  
  ngOnInit(): void {
    this.loadStats();
    setInterval(() => this.loadStats(), 30000);
  }
  
  loadStats(): void {
    this.parkingService.getDashboardStats().subscribe({
      next: (data: any) => {
        this.stats = data;
        console.log('Stats loaded:', data);
      },
      error: (error: any) => {
        console.error('Error loading stats:', error);
      }
    });
  }
  
  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
  
  goToSlots(): void {
    this.router.navigate(['/slots']);
  }
  
  goToVehicleEntry(): void {
    this.router.navigate(['/vehicle-entry']);
  }
goToReports(): void {
  this.router.navigate(['/reports']);
}
}
