import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ActivitySelectorComponent } from './activity-selector/activity-selector.component';

const routes: Routes = [
  { path: 'ActivitySelector', component: ActivitySelectorComponent },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
