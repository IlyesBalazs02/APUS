import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ActivityMapComponent } from './activity-map.component';

describe('ActivityMapComponent', () => {
  let component: ActivityMapComponent;
  let fixture: ComponentFixture<ActivityMapComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ActivityMapComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ActivityMapComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
