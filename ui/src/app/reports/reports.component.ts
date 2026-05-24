import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ParkingService } from '../services/parking.service';

@Component({
  selector: 'app-reports',
  templateUrl: './reports.component.html'
})
export class ReportsComponent implements OnInit {
  // Data from backend
  zoneStats: any[] = [];
  totalSlots = 0;
  occupiedSlots = 0;
  
  // Derived insights
  peakHour = 'Loading...';
  weeklyTrend = 'Loading...';
  bestZone = 'Loading...';
  recommendation = 'Loading...';
  
  // MLR formula explanation
  mlrFormula = 'Predicted = 0.5×Current + 0.3×Last3hAvg + 0.2×SameHourHistAvg';

  constructor(
    private parkingService: ParkingService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Fetch data when component loads
    this.parkingService.getDashboardStats().subscribe({
      next: (data) => {
        this.zoneStats = data.zoneStats || [];
        this.totalSlots = data.totalSlots;
        this.occupiedSlots = data.occupiedSlots;
        this.calculateInsights(data);
      },
      error: (err) => console.error('Failed to load stats', err)
    });
  }

  calculateInsights(data: any): void {
    // 1. Peak hour based on current time
    const hour = new Date().getHours();
    if (hour >= 8 && hour <= 10) this.peakHour = '8 AM – 10 AM (Morning Rush)';
    else if (hour >= 12 && hour <= 14) this.peakHour = '12 PM – 2 PM (Lunch Time)';
    else if (hour >= 17 && hour <= 19) this.peakHour = '5 PM – 7 PM (Evening Peak)';
    else this.peakHour = `${hour}:00 – ${hour+1}:00`;

    // 2. Weekly trend (simple day-of-week check)
    const day = new Date().getDay(); // 0=Sunday, 1=Monday, ..., 6=Saturday
    this.weeklyTrend = (day >= 1 && day <= 5) 
      ? 'Higher on weekdays (Mon-Fri)' 
      : 'Lower on weekends';

    // 3. Best zone (lowest occupancy)
    if (this.zoneStats && this.zoneStats.length) {
      const sorted = [...this.zoneStats].sort((a,b) => a.occupancyRate - b.occupancyRate);
      const best = sorted[0];
      this.bestZone = `Zone ${best.zoneName} (${best.occupancyRate}% occupied)`;
      
      // 4. Recommendation based on best zone
      if (best.zoneName === 'A') this.recommendation = 'Consider adding more slots in Zone A';
      else if (best.zoneName === 'B') this.recommendation = 'Optimise layout in Zone B';
      else this.recommendation = 'Zone C has the best availability – direct drivers there';
    }
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}
