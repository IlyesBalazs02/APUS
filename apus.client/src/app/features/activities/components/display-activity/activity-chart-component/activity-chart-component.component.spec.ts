import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ActivityChartComponentComponent } from './activity-chart-component.component';

describe('ActivityChartComponentComponent', () => {
  let component: ActivityChartComponentComponent;
  let fixture: ComponentFixture<ActivityChartComponentComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ActivityChartComponentComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ActivityChartComponentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
