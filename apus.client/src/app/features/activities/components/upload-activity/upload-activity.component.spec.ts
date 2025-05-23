import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UploadActivityComponent } from './upload-activity.component';

describe('UploadActivityComponent', () => {
  let component: UploadActivityComponent;
  let fixture: ComponentFixture<UploadActivityComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [UploadActivityComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(UploadActivityComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
