import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { ParkingService } from '../services/parking.service';

@Component({
  selector: 'app-parking-slots',
  templateUrl: './parking-slots.component.html'
})
export class ParkingSlotsComponent implements OnInit {
  slots: any[] = [];
  isLoading = false;
  
  constructor(
    private authService: AuthService,
    private parkingService: ParkingService,
    private router: Router
  ) {}
  
  ngOnInit(): void {
    this.loadSlots();
  }
  
  loadSlots(): void {
    this.isLoading = true;
    this.parkingService.getAllSlots().subscribe({
      next: (data: any) => {
        this.slots = data;
        this.isLoading = false;
        console.log('Slots loaded:', data.length);
      },
      error: (error: any) => {
        console.error('Error loading slots:', error);
        this.isLoading = false;
      }
    });
  }
  
  onSlotClick(slot: any): void {
    if (slot.status === 'Occupied') {
      if (confirm(`Exit vehicle from slot ${slot.slotNumber}?`)) {
        this.parkingService.vehicleExit(slot.slotId).subscribe({
          next: () => {
            alert('Vehicle exited successfully');
            this.loadSlots();
          },
          error: (error: any) => {
            alert('Error: ' + (error.error?.message || 'Failed to exit'));
          }
        });
      }
    } else if (slot.status === 'Available') {
      this.router.navigate(['/vehicle-entry'], { 
        queryParams: { slotId: slot.slotId, slotNumber: slot.slotNumber } 
      });
    } else {
      alert(`Slot ${slot.slotNumber} is ${slot.status}`);
    }
  }
  
  initializeSlots(): void {
    const token = this.authService.getToken();
    fetch('http://localhost:5219/api/parking/init-slots', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    }).then(response => {
      if (response.ok) {
        alert('Slots initialized successfully!');
        this.loadSlots();
      } else {
        alert('Failed to initialize slots');
      }
    }).catch(error => {
      console.error('Error:', error);
      alert('Error initializing slots');
    });
  }
  
  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
  
  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
