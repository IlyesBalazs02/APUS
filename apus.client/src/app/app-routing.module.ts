import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ActivitySelectorComponent } from './activities/components/activity-selector/activity-selector.component';
import { ListActivitiesComponent } from './activities/components/list-activities/list-activities.component';
import { ListActivitiesDifferentApproachComponent } from './activities/components/list-activities-different-approach/list-activities-different-approach.component';
import { BrowserModule } from '@angular/platform-browser';
import { CreateActivityComponent } from './activities/components/create-activity/create-activity.component';
import { MapSandBoxComponent } from './Testing/map-sand-box/map-sand-box.component';
import { ListActivitiesNewMethodComponent } from './activities/components/list-activities-new-method/list-activities-new-method.component';
import { UploadGpxFileComponent } from './Testing/upload-gpx-file/upload-gpx-file.component';
import { UploadActivityComponent } from './activities/components/upload-activity/upload-activity.component';

const routes: Routes = [
  { path: 'ActivitySelector', component: ActivitySelectorComponent },
  { path: 'ListActivities', component: ListActivitiesComponent },
  { path: 'ListActivities2', component: ListActivitiesDifferentApproachComponent },
  { path: 'createactivity', component: CreateActivityComponent },
  { path: 'map', component: MapSandBoxComponent },
  { path: 'ListActivities3', component: ListActivitiesNewMethodComponent },
  { path: 'uploadgpx', component: UploadActivityComponent },
  { path: '**', redirectTo: 'ListActivities', pathMatch: 'full' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes), BrowserModule],
  exports: [RouterModule]
})
export class AppRoutingModule { }
