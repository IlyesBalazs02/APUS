import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GroupsPostComponent } from './groups-post.component';

describe('GroupsPostComponent', () => {
  let component: GroupsPostComponent;
  let fixture: ComponentFixture<GroupsPostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [GroupsPostComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GroupsPostComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
