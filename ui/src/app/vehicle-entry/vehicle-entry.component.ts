import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { ParkingService } from '../services/parking.service';

@Component({
  selector: 'app-vehicle-entry',
  templateUrl: './vehicle-entry.component.html'
})
export class VehicleEntryComponent implements OnInit {
  vehicleNumber = '';
  selectedSlotId: number | null = null;
  selectedSlotNumber = '';
  availableSlots: any[] = [];
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  
  constructor(
    private authService: AuthService,
    private parkingService: ParkingService,
    private router: Router,
    private route: ActivatedRoute
  ) {}
  
  ngOnInit(): void {
    this.loadAvailableSlots();
    
    this.route.queryParams.subscribe(params => {
      if (params['slotId']) {
        this.selectedSlotId = parseInt(params['slotId']);
        this.selectedSlotNumber = params['slotNumber'];
      }
    });
  }
  
  loadAvailableSlots(): void {
    this.parkingService.getAllSlots().subscribe({
      next: (data: any) => {
        this.availableSlots = data.filter((slot: any) => slot.status === 'Available');
      },
      error: (error: any) => {
        console.error('Error loading slots:', error);
      }
    });
  }
  
  selectSlot(slot: any): void {
    this.selectedSlotId = slot.slotId;
    this.selectedSlotNumber = slot.slotNumber;
  }
  
  onSubmit(): void {
    if (!this.vehicleNumber.trim()) {
      this.errorMessage = 'Please enter vehicle number';
      return;
    }
    
    if (!this.selectedSlotId) {
      this.errorMessage = 'Please select a parking slot';
      return;
    }
    
    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    
    this.parkingService.vehicleEntry({
      slotId: this.selectedSlotId,
      vehicleNumber: this.vehicleNumber.toUpperCase()
    }).subscribe({
      next: () => {
        this.successMessage = `Vehicle assigned to slot ${this.selectedSlotNumber}`;
        this.isLoading = false;
        setTimeout(() => this.router.navigate(['/dashboard']), 1500);
      },
      error: (error: any) => {
        this.errorMessage = error.error?.message || 'Failed to register entry';
        this.isLoading = false;
      }
    });
  }
  
  goBack(): void {
    this.router.navigate(['/dashboard']);
  }
}
