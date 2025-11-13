import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GroupsRequestComponent } from './groups-request.component';

describe('GroupsRequestComponent', () => {
  let component: GroupsRequestComponent;
  let fixture: ComponentFixture<GroupsRequestComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [GroupsRequestComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GroupsRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
