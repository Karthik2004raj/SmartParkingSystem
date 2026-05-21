import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ParkingSlotsComponent } from './parking-slots.component';

describe('ParkingSlotsComponent', () => {
  let component: ParkingSlotsComponent;
  let fixture: ComponentFixture<ParkingSlotsComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [ParkingSlotsComponent]
    });
    fixture = TestBed.createComponent(ParkingSlotsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
