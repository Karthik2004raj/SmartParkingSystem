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
  zones: any[] = [];
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
      next: (data) => {
        this.slots = data;
        this.groupSlotsByZone();
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading slots:', error);
        this.isLoading = false;
        if (error.status === 401) this.logout();
      }
    });
  }

  groupSlotsByZone(): void {
    const zoneMap = new Map<string, any[]>();
    this.slots.forEach(slot => {
      if (!zoneMap.has(slot.zoneName)) {
        zoneMap.set(slot.zoneName, []);
      }
      zoneMap.get(slot.zoneName)!.push(slot);
    });
    this.zones = Array.from(zoneMap.entries()).map(([name, slots]) => ({
      name: name,
      slots: slots,
      occupiedCount: slots.filter(s => s.status === 'Occupied').length
    }));
  }

  onSlotClick(slot: any): void {
    if (slot.status === 'Occupied') {
      if (confirm(`Exit vehicle ${slot.currentVehicleNumber} from slot ${slot.slotNumber}?`)) {
        this.parkingService.vehicleExit(slot.slotId).subscribe({
          next: () => {
            alert('Vehicle exited successfully');
            this.loadSlots();
          },
          error: (error) => alert('Error: ' + (error.error?.message || 'Failed to exit'))
        });
      }
    } else if (slot.status === 'Available') {
      this.router.navigate(['/vehicle-entry'], { queryParams: { slotId: slot.slotId, slotNumber: slot.slotNumber } });
    } else {
      alert(`Slot ${slot.slotNumber} is ${slot.status.toLowerCase()} and cannot be used`);
    }
  }

  refresh(): void {
    this.loadSlots();
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  initializeSlots(): void {
    const token = this.authService.getToken();
    fetch('http://localhost:5042/api/parking/init-slots', {
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
}