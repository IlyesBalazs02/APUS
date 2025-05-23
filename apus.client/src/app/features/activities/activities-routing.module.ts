import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AuthGuard } from '../../core/guards/auth.guard';
import { DisplayActivitiesComponent } from './components/display-activities/display-activities.component';
import { CreateActivityComponent } from './components/create-activity/create-activity.component';
import { UploadActivityComponent } from './components/upload-activity/upload-activity.component';
import { DisplayActivityComponent } from './components/display-activity/display-activity.component';
import { EditActivityComponent } from './components/edit-activity/edit-activity.component';
import { ActivityMapComponent } from './components/display-activity/activity-map/activity-map.component';

const routes: Routes = [
    {
        path: '',
        canActivateChild: [AuthGuard],
        children: [
            { path: '', component: DisplayActivitiesComponent }, // /activities
            { path: 'create', component: CreateActivityComponent },    // /activities/create
            { path: 'upload', component: UploadActivityComponent },    // /activities/upload
            { path: ':id', component: DisplayActivityComponent },   // /activities/42
            { path: ':id/edit', component: EditActivityComponent },      // /activities/42/edit
            { path: ':id/map', component: ActivityMapComponent }        // /activities/42/map
        ]
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class ActivitiesRoutingModule { }
