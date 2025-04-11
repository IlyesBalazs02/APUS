import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ListActivitiesDifferentApproachComponent } from './list-activities-different-approach.component';

describe('ListActivitiesDifferentApproachComponent', () => {
  let component: ListActivitiesDifferentApproachComponent;
  let fixture: ComponentFixture<ListActivitiesDifferentApproachComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ListActivitiesDifferentApproachComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ListActivitiesDifferentApproachComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
