import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RunningActivityComponent } from './running-activity.component';

describe('RunningActivityComponent', () => {
  let component: RunningActivityComponent;
  let fixture: ComponentFixture<RunningActivityComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [RunningActivityComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RunningActivityComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
