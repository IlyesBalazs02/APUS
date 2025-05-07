import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MapSandBoxComponent } from './map-sand-box.component';

describe('MapSandBoxComponent', () => {
  let component: MapSandBoxComponent;
  let fixture: ComponentFixture<MapSandBoxComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [MapSandBoxComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MapSandBoxComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
