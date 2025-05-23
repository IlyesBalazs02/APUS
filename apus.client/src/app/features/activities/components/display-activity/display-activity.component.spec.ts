import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DisplayActivityComponent } from './display-activity.component';

describe('DisplayActivityComponent', () => {
  let component: DisplayActivityComponent;
  let fixture: ComponentFixture<DisplayActivityComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [DisplayActivityComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DisplayActivityComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
