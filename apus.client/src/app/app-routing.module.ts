import { Component, NgModule } from '@angular/core';
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
import { HomeComponentComponent } from './home-component/home-component.component';
import { AuthGuard } from './guards/auth.guard';
import { LoginComponent } from './auth/login/login.component';
import { RegisterComponent } from './auth/register/register.component';

const routes: Routes = [
  { path: '', component: HomeComponentComponent, canActivate: [AuthGuard] },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'createactivity', component: CreateActivityComponent, canActivate: [AuthGuard] },
  { path: 'map', component: MapSandBoxComponent, canActivate: [AuthGuard] },
  { path: 'uploadgpx', component: UploadActivityComponent, canActivate: [AuthGuard] },
  { path: 'DisplayActivities', component: DisplayActivitiesComponent, canActivate: [AuthGuard] },
  { path: 'DisplayActivity/:id', component: DisplayActivityComponent, canActivate: [AuthGuard] },
  { path: 'test1', component: UploadGpxFileComponent },
  { path: 'EditActivity/:id', component: EditActivityComponent, canActivate: [AuthGuard] },
  { path: 'ActivityMap', component: ActivityMapComponent, canActivate: [AuthGuard] },
  { path: '**', redirectTo: '' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes), BrowserModule],
  exports: [RouterModule]
})
export class AppRoutingModule { }
