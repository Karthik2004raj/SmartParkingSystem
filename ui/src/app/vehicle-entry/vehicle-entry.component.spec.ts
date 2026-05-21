import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VehicleEntryComponent } from './vehicle-entry.component';

describe('VehicleEntryComponent', () => {
  let component: VehicleEntryComponent;
  let fixture: ComponentFixture<VehicleEntryComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [VehicleEntryComponent]
    });
    fixture = TestBed.createComponent(VehicleEntryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
