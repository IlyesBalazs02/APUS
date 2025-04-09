import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ActivitySelectorComponent } from './activity-selector/activity-selector.component';
import { ListActivitiesComponent } from './list-activities/list-activities.component';

const routes: Routes = [
  { path: 'ActivitySelector', component: ActivitySelectorComponent },
  { path: 'ListActivities', component: ListActivitiesComponent },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
