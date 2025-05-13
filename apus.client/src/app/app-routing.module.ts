import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { BrowserModule } from '@angular/platform-browser';
import { CreateActivityComponent } from './activities/components/create-activity/create-activity.component';
import { MapSandBoxComponent } from './Testing/map-sand-box/map-sand-box.component';
import { UploadGpxFileComponent } from './Testing/upload-gpx-file/upload-gpx-file.component';
import { UploadActivityComponent } from './activities/components/upload-activity/upload-activity.component';
import { DisplayActivitiesComponent } from './activities/components/display-activities/display-activities.component';
import { DisplayActivityComponent } from './activities/components/display-activity/display-activity.component';
import { EditActivityComponent } from './activities/components/edit-activity/edit-activity.component';
import { ActivityMapComponent } from './activities/components/display-activity/activity-map/activity-map.component';

const routes: Routes = [
  { path: 'createactivity', component: CreateActivityComponent },
  { path: 'map', component: MapSandBoxComponent },
  { path: 'uploadgpx', component: UploadActivityComponent },
  { path: 'DisplayActivities', component: DisplayActivitiesComponent },
  { path: 'DisplayActivity/:id', component: DisplayActivityComponent },
  { path: 'test1', component: UploadGpxFileComponent },
  { path: 'EditActivity/:id', component: EditActivityComponent },
  { path: 'ActivityMap', component: ActivityMapComponent },
  { path: '**', redirectTo: 'DisplayActivities', pathMatch: 'full' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes), BrowserModule],
  exports: [RouterModule]
})
export class AppRoutingModule { }
