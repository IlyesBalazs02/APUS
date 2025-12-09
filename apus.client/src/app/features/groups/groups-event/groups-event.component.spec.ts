import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GroupsEventComponent } from './groups-event.component';

describe('GroupsEventComponent', () => {
  let component: GroupsEventComponent;
  let fixture: ComponentFixture<GroupsEventComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [GroupsEventComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GroupsEventComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
