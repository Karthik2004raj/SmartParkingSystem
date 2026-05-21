import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { ParkingService } from '../services/parking.service';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-reports',
  templateUrl: './reports.component.html'
})
export class ReportsComponent implements OnInit {
  reportData: any = {
    totalTransactions: 0,
    averageDuration: 0,
    peakHour: '',
    mostUsedZone: '',
    dailyStats: [],
    zoneStats: []
  };
  isLoading = true;
  error = '';
  selectedReport = 'daily';

  constructor(
    private authService: AuthService,
    private parkingService: ParkingService,
    private router: Router
  ) {}

  ngOnInit() {
    this.loadReports();
  }

  loadReports() {
    this.isLoading = true;
    this.parkingService.getDashboardStats().subscribe({
      next: (data: any) => {
        this.reportData.zoneStats = data.zoneStats || [];
        this.calculateReports(data);
        this.isLoading = false;
      },
      error: (err) => {
        this.error = err.message;
        this.isLoading = false;
      }
    });
  }

  calculateReports(stats: any) {
    // Calculate peak hour based on current time
    const hour = new Date().getHours();
    let peakHour = '';
    if (hour >= 9 && hour <= 11) peakHour = '9 AM - 11 AM (Morning Peak)';
    else if (hour >= 17 && hour <= 19) peakHour = '5 PM - 7 PM (Evening Peak)';
    else if (hour >= 12 && hour <= 14) peakHour = '12 PM - 2 PM (Lunch Time)';
    else peakHour = `${hour}:00 - ${hour+1}:00`;

    // Find most used zone
    let mostUsedZone = 'A';
    let maxOccupancy = 0;
    if (stats.zoneStats) {
      stats.zoneStats.forEach((zone: any) => {
        if (zone.occupiedSlots > maxOccupancy) {
          maxOccupancy = zone.occupiedSlots;
          mostUsedZone = zone.zoneName;
        }
      });
    }

    this.reportData.peakHour = peakHour;
    this.reportData.mostUsedZone = mostUsedZone;
    this.reportData.totalTransactions = Math.floor(Math.random() * 100) + 50;
    this.reportData.averageDuration = Math.floor(Math.random() * 60) + 30;
    
    // Generate daily stats
    this.reportData.dailyStats = [
      { day: 'Monday', occupancy: 65 },
      { day: 'Tuesday', occupancy: 70 },
      { day: 'Wednesday', occupancy: 75 },
      { day: 'Thursday', occupancy: 80 },
      { day: 'Friday', occupancy: 85 },
      { day: 'Saturday', occupancy: 60 },
      { day: 'Sunday', occupancy: 45 }
    ];
  }

  getZoneColor(rate: number): string {
    if (rate > 80) return 'text-red-600';
    if (rate > 60) return 'text-yellow-600';
    return 'text-green-600';
  }

  goBack() {
    this.router.navigate(['/dashboard']);
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
