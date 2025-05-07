import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UploadGpxFileComponent } from './upload-gpx-file.component';

describe('UploadGpxFileComponent', () => {
  let component: UploadGpxFileComponent;
  let fixture: ComponentFixture<UploadGpxFileComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [UploadGpxFileComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(UploadGpxFileComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
