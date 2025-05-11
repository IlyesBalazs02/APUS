import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ActivitySelectorComponent } from './activities/components/activity-selector/activity-selector.component';
import { BrowserModule } from '@angular/platform-browser';
import { CreateActivityComponent } from './activities/components/create-activity/create-activity.component';
import { MapSandBoxComponent } from './Testing/map-sand-box/map-sand-box.component';
import { UploadGpxFileComponent } from './Testing/upload-gpx-file/upload-gpx-file.component';
import { UploadActivityComponent } from './activities/components/upload-activity/upload-activity.component';
import { DisplayActivitiesComponent } from './activities/components/display-activities/display-activities.component';

const routes: Routes = [
  { path: 'ActivitySelector', component: ActivitySelectorComponent },
  { path: 'createactivity', component: CreateActivityComponent },
  { path: 'map', component: MapSandBoxComponent },
  { path: 'uploadgpx', component: UploadActivityComponent },
  { path: 'DisplayActivities', component: DisplayActivitiesComponent },
  { path: '**', redirectTo: 'ListActivities', pathMatch: 'full' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes), BrowserModule],
  exports: [RouterModule]
})
export class AppRoutingModule { }
