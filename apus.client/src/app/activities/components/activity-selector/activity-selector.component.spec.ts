import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ActivitySelectorComponent } from './activity-selector.component';

describe('ActivitySelectorComponent', () => {
  let component: ActivitySelectorComponent;
  let fixture: ComponentFixture<ActivitySelectorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ActivitySelectorComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ActivitySelectorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
