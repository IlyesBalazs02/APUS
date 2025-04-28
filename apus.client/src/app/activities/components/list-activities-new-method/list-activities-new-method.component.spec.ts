import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ListActivitiesNewMethodComponent } from './list-activities-new-method.component';

describe('ListActivitiesNewMethodComponent', () => {
  let component: ListActivitiesNewMethodComponent;
  let fixture: ComponentFixture<ListActivitiesNewMethodComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ListActivitiesNewMethodComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ListActivitiesNewMethodComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
