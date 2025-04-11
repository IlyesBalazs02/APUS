import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ActivitySelectorComponent } from './activity-selector/activity-selector.component';
import { ListActivitiesComponent } from './list-activities/list-activities.component';
import { ListActivitiesDifferentApproachComponent } from './list-activities-different-approach/list-activities-different-approach.component';
import { BrowserModule } from '@angular/platform-browser';

const routes: Routes = [
  { path: 'ActivitySelector', component: ActivitySelectorComponent },
  { path: 'ListActivities', component: ListActivitiesComponent },
  { path: 'ListActivities2', component: ListActivitiesDifferentApproachComponent },
];

@NgModule({
  imports: [RouterModule.forRoot(routes), BrowserModule],
  exports: [RouterModule]
})
export class AppRoutingModule { }
